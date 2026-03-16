using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticSwamp.AppLogic;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.Shared.Interfaces;
using SemanticSwamp.Shared.Prompts;

namespace SemanticKernel.MCP.Tools;

public sealed class SemanticSwampTools
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SemanticSwampTools(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<List<string>> ListDocumentUploadFilenamesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SemanticSwampDBContext>();

        return await db.DocumentUploads
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => x.FileName)
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentUploadInfoDto?> GetDocumentUploadByFilenameAsync(string fileName, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SemanticSwampDBContext>();

        var doc = await db.DocumentUploads
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.FileName == fileName, cancellationToken);

        if (doc is null) return null;

        return new DocumentUploadInfoDto
        {
            Id = doc.Id,
            FileName = doc.FileName,
            CreatedOn = doc.CreatedOn,
            IsActive = doc.IsActive,
            HasBeenProcessed = doc.HasBeenProcessed,
            Summary = doc.Summary,
            CollectionId = doc.CollectionId,
            CategoryId = doc.CategoryId,
        };
    }

    public async Task<string> ReadFileByDocUploadIdAsync(int documentUploadId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SemanticSwampDBContext>();

        var doc = await db.DocumentUploads
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == documentUploadId, cancellationToken);

        if (doc?.Base64Data is null) return "";

        var bytes = Convert.FromBase64String(doc.Base64Data);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    public async Task<List<RagSearchResultDto>> SearchUploadedDocumentsAsync(string question, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var rag = scope.ServiceProvider.GetRequiredService<IRAGManager>();

        var results = await rag.Search(question);

        return results.Select(x => new RagSearchResultDto
        {
            Id = x.Id,
            DocumentUploadId = x.DocumentUploadId,
            CategoryId = x.CategoryId,
            CollectionId = x.CollectionId,
            FileName = x.FileName,
            Terms = x.Terms,
            Text = x.Text,
            Index = x.Index,
            CreatedOn = x.CreatedOn,
        }).ToList();
    }

    public async Task<string> SummarizeDocumentAsync(int documentUploadId, string? overrideText, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SemanticSwampDBContext>();
        var chat = scope.ServiceProvider.GetRequiredService<IChatCompletionService>();
        var textManager = scope.ServiceProvider.GetRequiredService<ITextManager>();

        var doc = await db.DocumentUploads
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == documentUploadId, cancellationToken);

        if (doc?.Base64Data is null && string.IsNullOrEmpty(overrideText)) return "";

        var text = !string.IsNullOrEmpty(overrideText)
            ? overrideText
            : textManager.GetTextFileContent(doc!.Base64Data!);

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(Prompts.SummarizeText);

        var pieces = textManager.GetChunks(text);
        for (int i = 0; i < pieces.Count; i++)
        {
            chatHistory.AddUserMessage($" Text Section[{i}] - {pieces[i]} - End Text Section[{i}] ");
        }
        if (pieces.Count == 0)
        {
            chatHistory.AddUserMessage("Text to summarize: " + text + " --- end of text to summarize");
        }

        var reply = await chat.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
        return reply.Content ?? "";
    }

    public async Task<string> ExtractPdfTextAsync(int documentUploadId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SemanticSwampDBContext>();
        var chat = scope.ServiceProvider.GetRequiredService<IChatCompletionService>();

        var doc = await db.DocumentUploads
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == documentUploadId, cancellationToken);

        if (doc?.Base64Data is null) return "";

        // Reuse existing PDFManager logic (PdfPig extraction + AI reading-order reconstruction).
        var pdfManager = new PDFManager(chat);
        return await pdfManager.GetContent(doc.Base64Data);
    }

    public async Task<RagUploadResultDto> UploadDocumentToRagAsync(int documentUploadId, string? overrideText, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SemanticSwampDBContext>();
        var rag = scope.ServiceProvider.GetRequiredService<IRAGManager>();

        var doc = await db.DocumentUploads
            .Include(x => x.DocumentUploadTerms)
            .FirstOrDefaultAsync(x => x.Id == documentUploadId, cancellationToken);

        if (doc is null) return new RagUploadResultDto { Success = false };

        await rag.UploadToRAG(doc, overrideText);

        doc.HasBeenProcessed = true;
        await db.SaveChangesAsync(cancellationToken);

        return new RagUploadResultDto { Success = true };
    }
}
