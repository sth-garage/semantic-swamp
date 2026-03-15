using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared;
using SemanticSwamp.Shared.DTOs;

namespace SemanticSwamp.Blazor.Services;

/// <summary>
/// Scoped service that provides read access to reference data stored in SQL Server.
///
/// This service exists so that Blazor components never inject the EF DbContext directly.
/// Injecting a DbContext into a component is problematic because Blazor Server components
/// can be long-lived (for the duration of the SignalR circuit), whereas a DbContext is
/// designed to be short-lived. EntityService acts as a thin facade — it performs the
/// necessary queries and returns plain CLR objects, keeping the DbContext interaction
/// contained and the component code clean.
///
/// All methods are synchronous because the data sets are small and the overhead of async
/// EF calls (state machine allocation, continuation scheduling) outweighs the benefit
/// at this scale. Revisit if query times become noticeable.
/// </summary>
public class EntityService
{
    private readonly SemanticSwampDBContext _context;

    public EntityService(SemanticSwampDBContext context)
    {
        _context = context;
    }

    /// <summary>Returns all document collections, used to populate the collection dropdown in UploadModal.</summary>
    public List<Collection> GetCollections() => _context.Collections.ToList();

    /// <summary>Returns all categories, used to populate the category dropdown in UploadModal.</summary>
    public List<Category> GetCategories() => _context.Categories.ToList();

    /// <summary>Returns all terms, used to render the term-tagging checkboxes in UploadModal.</summary>
    public List<Term> GetTerms() => _context.Terms.ToList();

    /// <summary>
    /// Returns the display names of the built-in local test files defined by the LocalFileTypes enum.
    /// These are pre-existing text files on the server used to test the upload pipeline without
    /// needing to pick a file from the browser.
    /// </summary>
    public List<string> GetLocalFileTypes() =>
        new()
        {
            Enums.LocalFileTypes.Top5Movies.ToString(),
            Enums.LocalFileTypes.SportsHistory.ToString(),
            Enums.LocalFileTypes.TheOdyssey.ToString()
        };

    /// <summary>
    /// Returns all document upload records enriched with their collection and category names,
    /// ready for display in the DocumentsModal table.
    ///
    /// The DocumentUploads table stores only foreign-key IDs for collection and category, so
    /// this method resolves those to human-readable names by looking them up from the in-memory
    /// lists. The CreatedOn datetime is formatted as "yyyyMMdd_HHmmss" to match the format
    /// used when the document was originally saved, allowing it to be parsed back in the modal.
    /// </summary>
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
