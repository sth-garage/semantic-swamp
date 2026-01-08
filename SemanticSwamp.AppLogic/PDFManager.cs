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
    public class PDFManager : IPDFManager
    {

        private IChatCompletionService _chatCompletionService;

        public PDFManager(IChatCompletionService chatCompletionService)
        {
            _chatCompletionService = chatCompletionService;
        }

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

            return result;
        }

        public List<PDFText> GetPDFText(DocumentUpload documentUpload)
        {
            return GetPDFText(documentUpload.Base64Data);
        }

        public List<PDFText> GetPDFText(string base64Data)
        {
            var bytes = Convert.FromBase64String(base64Data);

            return GetPDFText(bytes);
        }

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
