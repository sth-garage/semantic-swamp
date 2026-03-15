using LLama.Common;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.Interfaces;
using SemanticSwamp.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Writer;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;

namespace SemanticSwamp.AppLogic
{
    /// <summary>
    /// Extracts text from PDF documents and uses AI to reconstruct the logical reading order
    /// of content that may appear out of sequence due to multi-column layouts, images, or
    /// non-linear text streams within the PDF's content structure.
    /// <para>
    /// Extraction flow:
    /// <list type="number">
    ///   <item>Decode the Base64 PDF to raw bytes.</item>
    ///   <item>Open with PdfPig and iterate every page, capturing text via <c>ContentOrderTextExtractor</c>.</item>
    ///   <item>Send each page's text to the AI with a prompt explaining multi-column interleaving,
    ///         instructing it to reconstruct the correct reading order without omitting any content.</item>
    ///   <item>Return the AI-corrected full-text string for use in summarisation and RAG indexing.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class PDFManager : IPDFManager
    {

        private IChatCompletionService _chatCompletionService;

        /// <summary>
        /// Initialises <see cref="PDFManager"/> with the Semantic Kernel chat completion service
        /// used to correct column-interleaved text extracted from each PDF page.
        /// </summary>
        /// <param name="chatCompletionService">The AI chat completion service registered via Semantic Kernel.</param>
        public PDFManager(IChatCompletionService chatCompletionService)
        {
            _chatCompletionService = chatCompletionService;
        }

        /// <summary>
        /// End-to-end convenience method: decodes a Base64 PDF, extracts per-page text with PdfPig,
        /// then sends those pages to the AI for column-order correction.
        /// This is the primary entry point called by <c>UploadManager</c> during PDF processing.
        /// </summary>
        /// <param name="base64Data">Base64-encoded PDF file content, as stored in <c>DocumentUpload.Base64Data</c>.</param>
        /// <returns>A single AI-corrected plain-text string representing the entire PDF's content.</returns>
        public async Task<string> GetContent(string base64Data)
        {
            var text = GetPDFText(base64Data);
            var result = await GetContent(text);
            return result;
        }

        /// <summary>
        /// Sends pre-extracted per-page text to the AI for logical reading-order reconstruction.
        /// <para>
        /// PDF text extractors output characters in the order they appear in the content stream,
        /// which for multi-column documents means columns may be interleaved (all of column 1,
        /// then all of column 2, etc.). The AI prompt explains this problem with a concrete example
        /// and instructs the model to reconstruct the correct reading order while preserving ALL content
        /// (this is explicitly NOT a summarisation step).
        /// </para>
        /// </summary>
        /// <param name="pdfTexts">List of per-page text objects produced by <see cref="GetPDFText(byte[])"/>.</param>
        /// <returns>A single string with the AI-corrected full text of the document.</returns>
        public async Task<string> GetContent(List<PDFText> pdfTexts)
        {
            var result = "";

            var prompt = @"You will be given a single page of a PDF document.  You need to extract all text information.  
                Each entry has a page number and the text on that page.  
                It is important to note that the text is not necessarily sequential.  
                If there are columns or images, the text may be out of order.  
                Read the entire text and then provide a text equivalent.  
                Text or paragraphs may span multiple pages or columns.  

                Example:
                Column 1                    Column 2
                word: meaning of word 1     word2: meaning of word 2
                is getting a coffee.        is getting a magazine.

                Could be presented to you as:

                word
                word2

                meaning of word1 is getting a coffee
                meaning of word2 is getting a magazine

                The expected text back would be something like:

                word: meaning of word1 is getting a coffee
                word2: meaning of word2 is getting a magazine

                Lists MUST include ALL entries.  
                The result should NOT be a summary - it needs include ALL information.";


            ChatHistory chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);

            foreach (var pdfText in pdfTexts)
            {
                
                chatHistory.AddUserMessage(String.Format("Page {0} - Content {1}", pdfText.PageNumber, pdfText.Text));

            }
            var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory);

            result = response.Content;

            return result;
        }

        /// <summary>
        /// Convenience overload that extracts per-page text from the PDF stored in a
        /// <see cref="DocumentUpload"/> entity's <c>Base64Data</c> field.
        /// Delegates to the Base64 string overload.
        /// </summary>
        /// <param name="documentUpload">The document entity whose <c>Base64Data</c> contains the raw PDF bytes.</param>
        /// <returns>A list of <see cref="PDFText"/> objects, one per page.</returns>
        public List<PDFText> GetPDFText(DocumentUpload documentUpload)
        {
            return GetPDFText(documentUpload.Base64Data);
        }

        /// <summary>
        /// Decodes a Base64 PDF string to raw bytes and delegates to the byte-array overload.
        /// </summary>
        /// <param name="base64Data">Base64-encoded PDF file content.</param>
        /// <returns>A list of <see cref="PDFText"/> objects, one per page.</returns>
        public List<PDFText> GetPDFText(string base64Data)
        {
            var bytes = Convert.FromBase64String(base64Data);

            return GetPDFText(bytes);
        }

        /// <summary>
        /// Core extraction method: opens the raw PDF bytes with PdfPig, iterates every page,
        /// and collects text using <c>ContentOrderTextExtractor.GetText</c> (which attempts to
        /// preserve reading order). Also captures word-based and raw stream text for reference
        /// (though only the <c>ContentOrderTextExtractor</c> result is stored in the returned list).
        /// </summary>
        /// <param name="bytes">Raw PDF file bytes.</param>
        /// <returns>A list of <see cref="PDFText"/> objects with 1-based page numbers and extracted text.</returns>
        public List<PDFText> GetPDFText(byte[] bytes)
        {
            var result = new List<PDFText>();

            using (var pdf = PdfDocument.Open(bytes))
            {
                //string result = "";

                var pages = pdf.GetPages().ToList();

                for (var i = 0; i < pages.Count(); i++)
                {
                    var page = pages[i];

                    // Either extract based on order in the underlying document with newlines and spaces.
                    var text = ContentOrderTextExtractor.GetText(page);

                    // Or based on grouping letters into words.
                    var otherText = string.Join(" ", page.GetWords());

                    // Or the raw text of the page's content stream.
                    var rawText = page.Text;

                    //Console.WriteLine(text);

                    result.Add(new PDFText
                    {
                        Text = text,
                        PageNumber = (i + 1)
                    });
                }


                return result;

            }
        }
    }

    
}
