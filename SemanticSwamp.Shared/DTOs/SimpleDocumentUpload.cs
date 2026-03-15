namespace SemanticSwamp.Shared.DTOs
{
    /// <summary>
    /// A flat read-model returned by EntityService.GetDocuments() for display in the
    /// DocumentsModal table. It combines fields from the DocumentUpload entity with the
    /// resolved names of its related Collection and Category so the UI only needs a single
    /// list fetch — no further lookups are required at render time.
    ///
    /// CreatedOn is stored as a pre-formatted string ("yyyyMMdd_HHmmss") to match the format
    /// used when the record was saved. DocumentsModal.razor parses it back to DateTime using
    /// TryParseExact with the same format mask.
    /// </summary>
    public class SimpleDocumentUpload
    {
        /// <summary>Resolved name of the Collection this document belongs to.</summary>
        public string CollectionName { get; set; }

        /// <summary>Resolved name of the Category this document belongs to.</summary>
        public string CategoryName { get; set; }

        /// <summary>Upload timestamp formatted as "yyyyMMdd_HHmmss" for consistent parsing.</summary>
        public string CreatedOn { get; set; }

        /// <summary>Whether the document is currently active (soft-delete flag).</summary>
        public bool IsActive { get; set; } = false;

        /// <summary>Original filename as uploaded by the user.</summary>
        public string FileName { get; set; }

        /// <summary>True once the document has been chunked and vectorised into Qdrant.</summary>
        public bool HasBeenProcessed { get; set; }

        /// <summary>AI-generated plain-text summary, shown in the expandable row of the documents table.</summary>
        public string Summary { get; set; }

        /// <summary>Database primary key, used to construct the /api/download/{id} URL.</summary>
        public int Id { get; set; }
    }
}
