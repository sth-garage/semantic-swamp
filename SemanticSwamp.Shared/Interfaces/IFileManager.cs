using Microsoft.AspNetCore.Http;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Text;
using static SemanticSwamp.Shared.Enums;

namespace SemanticSwamp.Shared.Interfaces
{
    /// <summary>
    /// Defines the contract for managing file uploads and generating AI text summaries.
    /// The primary implementation is <c>UploadManager</c> in the AppLogic project, which
    /// orchestrates the full pipeline: metadata extraction → DB persistence → AI summary → RAG vectorisation.
    /// </summary>
    public interface IFileManager
    {
        /// <summary>
        /// Processes a full file upload end-to-end: saves metadata, resolves collection/category/terms,
        /// persists the record to the database, generates an AI summary, and uploads chunks to the RAG vector store.
        /// </summary>
        /// <param name="fileUploadDTO">DTO carrying the uploaded file plus user-selected metadata (collection, category, terms).</param>
        /// <returns>The persisted <see cref="DocumentUpload"/> entity with its summary populated.</returns>
        Task<DocumentUpload> ProcessUpload(FileUploadDTO fileUploadDTO);

        /// <summary>
        /// Sends the text content (provided as Base64) to the AI chat completion service and returns a plain-text summary.
        /// The content is first chunked; each chunk is added as a separate user message so large documents
        /// do not exceed the model's context window.
        /// </summary>
        /// <param name="base64Data">Base64-encoded text (or PDF-extracted text re-encoded as Base64).</param>
        /// <param name="isPDF">Reserved for future per-format prompt adjustments; currently unused in the summary prompt.</param>
        /// <returns>A plain-text AI-generated summary of the document.</returns>
        Task<string> GetTextSummary(string base64Data, bool isPDF = false);

        /// <summary>
        /// Convenience overload that reads a pre-bundled sample file from the <c>/SampleData/</c> directory
        /// and returns an AI summary. Useful for smoke-testing the summarisation pipeline during development.
        /// </summary>
        /// <param name="localFileTypes">Enum value that maps to a specific sample file on disk.</param>
        /// <returns>A plain-text AI-generated summary of the chosen sample file.</returns>
        Task<string> GetTextFileSummaryFromPath(LocalFileTypes localFileTypes);
    }
}
