using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticSwamp.Shared.Interfaces;
using SemanticSwamp.Shared.Prompts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticSwamp.Shared.Utility
{
    /// <summary>
    /// Provides utility helpers for converting file content to and from Base64,
    /// and for splitting large text documents into manageable chunks.
    /// <para>
    /// All document content is stored as Base64 in the database so that arbitrary binary
    /// files (PDFs, images, etc.) can be round-tripped safely through a text column.
    /// This class is the single place where that encoding/decoding logic lives.
    /// </para>
    /// </summary>
    public class TextManager : ITextManager
    {

        /// <summary>
        /// Decodes a Base64 string back to its original UTF-8 text.
        /// Called when the application needs to read the human-readable content
        /// of a stored document (e.g., before sending it to the AI for summarisation).
        /// </summary>
        /// <param name="base64Data">Base64-encoded UTF-8 string, as stored in <c>DocumentUpload.Base64Data</c>.</param>
        /// <returns>The decoded plain-text content of the file.</returns>
        public string GetTextFileContent(string base64Data)
        {
            var result = "";

            var bytes = Convert.FromBase64String(base64Data);
            result = System.Text.Encoding.UTF8.GetString(bytes);
            return result;
        }

        /// <summary>
        /// Splits a string into equal-length substrings of <paramref name="chunkSize"/> characters.
        /// Uses integer division, so any trailing characters that do not fill a complete chunk are silently dropped.
        /// Callers that require the last partial chunk should handle the remainder separately.
        /// </summary>
        /// <param name="str">The string to split.</param>
        /// <param name="chunkSize">The fixed character length of each chunk.</param>
        /// <returns>An enumerable of equally-sized substrings.</returns>
        public IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        /// <summary>
        /// Encodes a plain-text string to Base64 by first converting it to UTF-8 bytes.
        /// Used by <c>UploadManager</c> to convert AI-extracted PDF text back into Base64
        /// so it can flow through the same <c>GetTextSummary</c> path as non-PDF files.
        /// </summary>
        /// <param name="value">The plain-text string to encode.</param>
        /// <returns>Base64-encoded representation of the UTF-8 bytes.</returns>
        public string GetBase64DataFromString(string value)
        {
            var result = "";

            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            result = Convert.ToBase64String(bytes);

            return result;
        }


        /// <summary>
        /// Reads all bytes from an ASP.NET Core <see cref="IFormFile"/> (a multipart-uploaded file)
        /// into a <see cref="MemoryStream"/> and returns them as a Base64 string.
        /// This normalises uploaded file content into the Base64 format used for database storage.
        /// </summary>
        /// <param name="file">The uploaded file from an HTTP form post.</param>
        /// <returns>Base64-encoded file content, or an empty string if the file is null or empty.</returns>
        public async Task<string> GetBase64DataFromFile(IFormFile file)
        {
            var result = "";

            if (file == null || file.Length == 0)
            {
                result = "";
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                var bytes = stream.ToArray();
                result = Convert.ToBase64String(bytes);
            }

            return result;

        }

        /// <summary>
        /// Splits a long text string into chunks of up to <paramref name="chunkSize"/> characters
        /// so that each chunk can be submitted as a separate AI chat message without exceeding
        /// the model's context window limit.
        /// Delegates to <see cref="Split"/>, which uses integer division — the last partial
        /// chunk is omitted if the text length is not evenly divisible by <paramref name="chunkSize"/>.
        /// </summary>
        /// <param name="text">The full document text to chunk.</param>
        /// <param name="chunkSize">Maximum characters per chunk. Defaults to 10,000.</param>
        /// <returns>A list of text chunks ready to send to the AI.</returns>
        public List<string> GetChunks(string text, int chunkSize = 10000)
        {
            var result = Split(text, chunkSize).ToList();
            return result;
        }



    }
}
