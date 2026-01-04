// FilesController.cs
using Microsoft.AspNetCore.Mvc;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.DTOs;

[ApiController]
[Route("api/[controller]")]
public class DocumentUploadsController : ControllerBase
{
    private SemanticSwampDBContext _context;

    public DocumentUploadsController(SemanticSwampDBContext context)
    {
        _context = context;
    }

    [HttpGet("Simple")]
    public async Task<List<SimpleDocumentUpload>> GetSimple()
    {
        var result = new List<SimpleDocumentUpload>();

        foreach (var documentUpload in _context.DocumentUploads.ToList())
        {
            var categoryName = "<error>";
            var collectionName = "<error>";

            var category = _context.Categories.FirstOrDefault(x => x.Id == documentUpload.CategoryId);
            var collection = _context.Collections.FirstOrDefault(x => x.Id == documentUpload.CollectionId);


            result.Add(new SimpleDocumentUpload
            {
                CategoryName = category != null ? category.Name : categoryName,
                CollectionName = collection != null ? collection.Name : collectionName,
                FileName = documentUpload.FileName,
                CreatedOn = documentUpload.CreatedOn.ToString("yyyyMMdd_HHmmss"),
                HasBeenProcessed = documentUpload.HasBeenProcessed,
                IsActive = documentUpload.IsActive,
                Summary = documentUpload.Summary
            });
        }

        return result.ToList();
    }

    [HttpGet("DownloadOriginalDocument")]
    public async Task<IActionResult> DownloadOriginalDocument([FromQuery] DownloadLocalPayload downloadDocumentPayload)
    {
        // Basic security: Only allow downloading files from a specific folder
        var documentUpload = _context.DocumentUploads.FirstOrDefault(x => x.Id == downloadDocumentPayload.DocumentUploadId);

        if (documentUpload != null)
        {
            var base64StringData = documentUpload.Base64Data;
            var memory = new MemoryStream();
            using (var memStream = new MemoryStream(Convert.FromBase64String(base64StringData)))
            {
                await memStream.CopyToAsync(memory);

            }

            memory.Position = 0;
            var contentType = "application/octet-stream";
            return File(memory, contentType, documentUpload.FileName);
        }
        else
        {
            return Ok("");
        }
    }
}