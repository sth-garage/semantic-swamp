using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared;
using SemanticSwamp.Shared.DTOs;

namespace SemanticSwamp.Blazor.Services;

public class EntityService
{
    private readonly SemanticSwampDBContext _context;

    public EntityService(SemanticSwampDBContext context)
    {
        _context = context;
    }

    public List<Collection> GetCollections() => _context.Collections.ToList();

    public List<Category> GetCategories() => _context.Categories.ToList();

    public List<Term> GetTerms() => _context.Terms.ToList();

    public List<string> GetLocalFileTypes() =>
        new()
        {
            Enums.LocalFileTypes.Top5Movies.ToString(),
            Enums.LocalFileTypes.SportsHistory.ToString(),
            Enums.LocalFileTypes.TheOdyssey.ToString()
        };

    public List<SimpleDocumentUpload> GetDocuments()
    {
        var result = new List<SimpleDocumentUpload>();
        foreach (var doc in _context.DocumentUploads.ToList())
        {
            var category   = _context.Categories.FirstOrDefault(x => x.Id == doc.CategoryId);
            var collection = _context.Collections.FirstOrDefault(x => x.Id == doc.CollectionId);
            result.Add(new SimpleDocumentUpload
            {
                CategoryName     = category?.Name   ?? "<error>",
                CollectionName   = collection?.Name ?? "<error>",
                FileName         = doc.FileName,
                CreatedOn        = doc.CreatedOn.ToString("yyyyMMdd_HHmmss"),
                HasBeenProcessed = doc.HasBeenProcessed,
                IsActive         = doc.IsActive,
                Summary          = doc.Summary,
                Id               = doc.Id,
            });
        }
        return result;
    }
}
