using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticSwamp.Shared.Interfaces
{
    /// <summary>
    /// Defines the contract for Base64 encoding/decoding utilities and text chunking.
    /// The primary implementation is <c>TextManager</c> in the Shared project.
    /// These helpers are used throughout the pipeline to move file content between
    /// binary storage (Base64 in the database) and plain text (for AI prompts and RAG indexing).
    /// </summary>
    public interface ITextManager
    {
        /// <summary>
        /// Decodes a Base64 string back to its UTF-8 text representation.
        /// Used when reading file content back out of the database for display or AI processing.
        /// </summary>
        /// <param name="base64Data">Base64-encoded UTF-8 text.</param>
        /// <returns>The decoded plain-text string.</returns>
        string GetTextFileContent(string base64Data);

        /// <summary>
        /// Reads all bytes from an uploaded <see cref="IFormFile"/> and returns them as a Base64 string.
        /// This is how file content is normalised for storage in the database.
        /// </summary>
        /// <param name="file">The HTTP multipart-uploaded file.</param>
        /// <returns>Base64-encoded file content.</returns>
        Task<string> GetBase64DataFromFile(IFormFile file);

        /// <summary>
        /// Splits a long text string into fixed-size chunks for batch AI processing.
        /// Chunking is necessary because large documents exceed a model's context window;
        /// each chunk is sent as a separate message in the chat history.
        /// Note: uses integer division, so the last partial chunk (if any) is dropped.
        /// </summary>
        /// <param name="text">The full text to split.</param>
        /// <param name="chunkSize">Maximum character count per chunk. Defaults to 10,000.</param>
        /// <returns>A list of equal-length text chunks.</returns>
        List<string> GetChunks(string text, int chunkSize = 10000);

        /// <summary>
        /// Encodes a plain-text string to Base64 (UTF-8 bytes → Base64).
        /// Used by <c>UploadManager</c> to convert AI-extracted PDF text back into the Base64
        /// format expected by <c>GetTextSummary</c> and the RAG pipeline.
        /// </summary>
        /// <param name="value">The plain-text string to encode.</param>
        /// <returns>Base64-encoded representation of the UTF-8 bytes.</returns>
        string GetBase64DataFromString(string value);
    }
}
