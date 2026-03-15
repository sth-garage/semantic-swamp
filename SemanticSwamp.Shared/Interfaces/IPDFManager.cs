using Microsoft.SemanticKernel.ChatCompletion;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticSwamp.Shared.Interfaces
{
    /// <summary>
    /// Defines the contract for PDF text extraction and AI-assisted content reconstruction.
    /// The primary implementation is <c>PDFManager</c> in the AppLogic project, which uses
    /// the PdfPig library for raw extraction and then sends each page to the AI to correct
    /// column-interleaved or out-of-order text before returning a logically ordered full-text string.
    /// </summary>
    public interface IPDFManager
    {
        /// <summary>
        /// Accepts pre-extracted per-page text objects, sends them to the AI with a column-correction
        /// prompt, and returns a single logically ordered plain-text string of the entire document.
        /// </summary>
        /// <param name="pdfTexts">List of <see cref="PDFText"/> objects, one per PDF page.</param>
        /// <returns>The full document text with column/layout artifacts corrected by the AI.</returns>
        Task<string> GetContent(List<PDFText> pdfTexts);

        /// <summary>
        /// Extracts per-page text from the PDF stored in a <see cref="DocumentUpload"/> entity's Base64Data field.
        /// </summary>
        /// <param name="documentUpload">The document entity whose <c>Base64Data</c> contains the raw PDF bytes.</param>
        /// <returns>A list of <see cref="PDFText"/> objects, one per page, with the page number and raw extracted text.</returns>
        List<PDFText> GetPDFText(DocumentUpload documentUpload);

        /// <summary>
        /// Extracts per-page text from a Base64-encoded PDF string.
        /// Decodes to bytes and delegates to the byte-array overload.
        /// </summary>
        /// <param name="base64Data">Base64-encoded PDF file content.</param>
        /// <returns>A list of <see cref="PDFText"/> objects, one per page.</returns>
        List<PDFText> GetPDFText(string base64Data);

        /// <summary>
        /// Core extraction method: opens the PDF byte array with PdfPig, iterates every page,
        /// and collects the text using <c>ContentOrderTextExtractor</c>.
        /// </summary>
        /// <param name="bytes">Raw PDF file bytes.</param>
        /// <returns>A list of <see cref="PDFText"/> objects, one per page.</returns>
        List<PDFText> GetPDFText(byte[] bytes);

        /// <summary>
        /// End-to-end convenience method: decodes a Base64 PDF, extracts page text with PdfPig,
        /// then passes the pages to the AI for column-order correction.
        /// This is the primary entry point called by <c>UploadManager</c> during document processing.
        /// </summary>
        /// <param name="base64Data">Base64-encoded PDF file content.</param>
        /// <returns>The full, AI-corrected plain-text content of the PDF.</returns>
        Task<string> GetContent(string base64Data);
    }
}
