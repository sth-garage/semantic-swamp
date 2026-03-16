using Microsoft.EntityFrameworkCore;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.DTOs;
using SemanticSwamp.Shared.Interfaces;

namespace SemanticSwamp.MCPBlazor.Services;

public class McpUploadManager : IFileManager
{
    private readonly SemanticSwampDBContext _context;
    private readonly ITextManager _textManager;
    private readonly IMcpClient _mcp;

    public McpUploadManager(SemanticSwampDBContext context, ITextManager textManager, IMcpClient mcp)
    {
        _context = context;
        _textManager = textManager;
        _mcp = mcp;
    }

    public async Task<string> GetTextSummary(string base64Data, bool isPDF = false)
    {
        // Maintain the legacy interface contract.
        // In this MCP-backed app, summarization is performed as part of ProcessUpload.
        // Callers can still decode base64 and return a local summary if desired.
        return "";
    }

    public async Task<DocumentUpload> ProcessUpload(FileUploadDTO fileUploadDTO)
    {
        var terms = await GetTerms(fileUploadDTO);
        await _context.SaveChangesAsync();

        var result = new DocumentUpload
        {
            IsActive = true,
            CreatedOn = DateTime.UtcNow,
            HasBeenProcessed = false,
        };

        result = await AddFileMetaData(result, fileUploadDTO);
        result = await SetCollection(result, fileUploadDTO);
        result = await SetCategory(result, fileUploadDTO);

        _context.DocumentUploads.Add(result);
        await _context.SaveChangesAsync();

        await LinkTermsToDocumentUpload(terms, result);
        await _context.SaveChangesAsync();

        var isPdf = result.FileName.ToLowerInvariant().EndsWith("pdf");

        string? overrideText = null;
        if (isPdf)
        {
            overrideText = await _mcp.CallToolAsync<string>(
                "pdf_extract_text",
                new { documentUploadId = result.Id });
        }

        var summary = await _mcp.CallToolAsync<string>(
            "summarize_document",
            new { documentUploadId = result.Id, overrideText });

        result.Summary = summary;
        await _context.SaveChangesAsync();

        await _mcp.CallToolAsync<RagUploadResult>(
            "rag_upload_document",
            new { documentUploadId = result.Id, overrideText });

        // The MCP host sets HasBeenProcessed=true in the DB, but refresh locally as well.
        await _context.Entry(result).ReloadAsync();

        return result;
    }

    private async Task<DocumentUpload> AddFileMetaData(DocumentUpload documentUpload, FileUploadDTO fileUploadDTO)
    {
        documentUpload.Base64Data = await _textManager.GetBase64DataFromFile(fileUploadDTO.file);
        documentUpload.FileName = fileUploadDTO.file.FileName;
        return documentUpload;
    }

    private Task LinkTermsToDocumentUpload(List<Term> terms, DocumentUpload documentUpload)
    {
        foreach (var term in terms)
        {
            _context.DocumentUploadTerms.Add(new DocumentUploadTerm
            {
                TermId = term.Id,
                DocumentUploadId = documentUpload.Id
            });
        }

        return Task.CompletedTask;
    }

    private Task<DocumentUpload> SetCollection(DocumentUpload documentUpload, FileUploadDTO fileUploadDTO)
    {
        Collection? collection;

        collection = (!string.IsNullOrEmpty(fileUploadDTO.newCollectionName))
            ? new Collection { Name = fileUploadDTO.newCollectionName }
            : _context.Collections.FirstOrDefault(x => x.Id == fileUploadDTO.collectionId);

        documentUpload.Collection = collection!;
        return Task.FromResult(documentUpload);
    }

    private Task<DocumentUpload> SetCategory(DocumentUpload documentUpload, FileUploadDTO fileUploadDTO)
    {
        Category? category;

        category = (!string.IsNullOrEmpty(fileUploadDTO.newCategoryName))
            ? new Category { Name = fileUploadDTO.newCategoryName }
            : _context.Categories.FirstOrDefault(x => x.Id == fileUploadDTO.categoryId);

        documentUpload.Category = category!;
        return Task.FromResult(documentUpload);
    }

    private async Task<List<Term>> GetTerms(FileUploadDTO fileUploadDTO)
    {
        var termsList = new List<Term>();

        if (fileUploadDTO.termIds != null && fileUploadDTO.termIds.Count > 0)
        {
            foreach (var termId in fileUploadDTO.termIds)
            {
                int termIdValue = -1;
                int.TryParse(termId, out termIdValue);
                if (termIdValue >= 0)
                {
                    termIdValue++;
                    var term = await _context.Terms.FirstOrDefaultAsync(x => x.Id.Equals(termIdValue));
                    if (term != null) termsList.Add(term);
                }
            }
        }

        if (!string.IsNullOrEmpty(fileUploadDTO.newTermNames))
        {
            fileUploadDTO.newTermNames = fileUploadDTO.newTermNames.TrimStart('[').TrimEnd(']');
            var newTerms = fileUploadDTO.newTermNames.Split(",");
            foreach (var newTerm in newTerms)
            {
                var newTermEntity = new Term
                {
                    Name = newTerm.TrimStart('"').TrimEnd('"')
                };
                termsList.Add(newTermEntity);
                _context.Terms.Add(newTermEntity);
                await _context.SaveChangesAsync();
            }
        }

        return termsList;
    }

    private sealed class RagUploadResult
    {
        public bool Success { get; set; }
    }
}
