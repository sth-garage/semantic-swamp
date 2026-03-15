using Microsoft.AspNetCore.Http;

namespace SemanticSwamp.Shared.DTOs
{
    /// <summary>
    /// Data transfer object that carries all the information needed to process a single file upload.
    ///
    /// In Blazor, IBrowserFile cannot be sent over HTTP. Instead, the Blazor component reads the
    /// file into a MemoryStream and wraps it in a FormFile so it matches the IFormFile interface
    /// that UploadManager expects. This lets the same processing pipeline be used whether the
    /// caller is a Blazor component or a traditional HTTP controller.
    ///
    /// Collection / category handling:
    ///   - If the user selects an existing collection, collectionId is set and newCollectionName is empty.
    ///   - If the user types a new name, newCollectionName is set and collectionId is null.
    ///   UploadManager.SetCollection/SetCategory handles both cases.
    ///
    /// Term handling:
    ///   - termIds: existing term IDs (as strings) that the user checked.
    ///   - newTermNames: JSON-encoded array of brand-new term names the user typed.
    ///   UploadManager.GetTerms parses both and creates new Term entities as needed.
    /// </summary>
    public class FileUploadDTO
    {
        /// <summary>The uploaded file, wrapped as IFormFile so the server-side pipeline can read it.</summary>
        public IFormFile file { get; set; }

        /// <summary>ID of an existing Category to associate with this document. Null if creating a new one.</summary>
        public int? categoryId { get; set; } = 1;

        /// <summary>ID of an existing Collection to associate with this document. Null if creating a new one.</summary>
        public int? collectionId { get; set; } = 1;

        /// <summary>Name for a brand-new Collection. Takes precedence over collectionId when non-empty.</summary>
        public string? newCollectionName { get; set; } = "";

        /// <summary>Name for a brand-new Category. Takes precedence over categoryId when non-empty.</summary>
        public string? newCategoryName { get; set; } = "";

        /// <summary>
        /// JSON-serialised array of new term names typed by the user (e.g. '["RAG","embeddings"]').
        /// UploadManager parses this string, creates new Term entities, and links them to the upload.
        /// </summary>
        public string newTermNames { get; set; } = "";

        /// <summary>
        /// IDs of existing Terms to link to this document, provided as strings because they
        /// originate from HTML checkbox values. UploadManager parses each to int before lookup.
        /// </summary>
        public List<string> termIds { get; set; } = new List<string>();
    }

    /// <summary>
    /// Payload used by the legacy Web API controller to trigger processing of a local sample file
    /// by its enum name rather than requiring a browser file-pick.
    /// </summary>
    public class UploadLocalPayload
    {
        public string LocalFileName { get; set; }
    }

    /// <summary>
    /// Payload for the download endpoint in the legacy Web API controller, identifying
    /// which DocumentUpload record to retrieve by its database ID.
    /// </summary>
    public class DownloadLocalPayload
    {
        public int documentUploadId { get; set; }
    }
}


