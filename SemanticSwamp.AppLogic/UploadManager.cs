using Elastic.Clients.Elasticsearch.Aggregations;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.DTOs;
using SemanticSwamp.Shared.Interfaces;
using SemanticSwamp.Shared.Prompts;
using System.Collections;
using System.Text;
using System.Text.Unicode;
using static SemanticSwamp.Shared.Enums;

#pragma warning disable SKEXP0001 
namespace SemanticSwamp.AppLogic
{
    public class UploadManager : IFileManager
    {
        private SemanticSwampDBContext _context;
        private IChatCompletionService _chatCompletionService;
        private ITextManager _textManager;
        private IRAGManager _ragManager;
        private IPDFManager _pdfManager;

        public UploadManager(SemanticSwampDBContext context, IChatCompletionService chatCompletionService, ITextManager textManager, IRAGManager ragManager, IPDFManager pdfManager)
        {
            _context = context;
            _chatCompletionService = chatCompletionService;
            _textManager = textManager;
            _ragManager = ragManager;
            _pdfManager = pdfManager;
        }

        public async Task<DocumentUpload> ProcessUpload(FileUploadDTO fileUploadDTO)
        {
            //await _ragManager.Search("Who are some players on the Falcons?");
            var terms = await GetTerms(fileUploadDTO);
            await _context.SaveChangesAsync();

            var result = new DocumentUpload()
            {
                IsActive = true,
                CreatedOn = DateTime.UtcNow,
                HasBeenProcessed = false,
            };


            result = await AddFileMetaData(result, fileUploadDTO);

            //var text = _pdfManager.GetPDFText(result);
            //var content = await _pdfManager.GetContent(text);

            result = await SetCollection(result, fileUploadDTO);
            result = await SetCategory(result, fileUploadDTO);

            _context.DocumentUploads.Add(result);
            await _context.SaveChangesAsync();

            await LinkTermsToDocumentUpload(terms, result);
            await _context.SaveChangesAsync();

            var isPDF = result.FileName.ToLowerInvariant().EndsWith("pdf");

            //var summary = await GetTextSummary(result.Base64Data, isPDF);
            //result.Summary = summary;

            string base64ForSummary = result.Base64Data;
            string overrideText = null;

            if (isPDF)
            {
                var pdfContent = await _pdfManager.GetContent(result.Base64Data);
                base64ForSummary = _textManager.GetBase64DataFromString(pdfContent);
                overrideText = pdfContent;
            }

            var summary = await GetTextSummary(base64ForSummary, isPDF);
            result.Summary = summary;

            await _ragManager.UploadToRAG(result, overrideText);

            await _context.SaveChangesAsync();
            
            

            return result;
        }

        #region Process Helpers
        private async Task<DocumentUpload> AddFileMetaData(DocumentUpload documentUpload, FileUploadDTO fileUploadDTO)
        {
            var result = documentUpload;

            result.Base64Data = await _textManager.GetBase64DataFromFile(fileUploadDTO.file);
            result.FileName = fileUploadDTO.file.FileName;

            return result;
        }

        private async Task LinkTermsToDocumentUpload(List<Term> terms, DocumentUpload documentUpload)
        {
            foreach (var term in terms)
            {
                _context.DocumentUploadTerms.Add(new DocumentUploadTerm
                {
                    TermId = term.Id,
                    DocumentUploadId = documentUpload.Id
                });
            }
        }

        

        #endregion

        #region Entities

        private async Task<DocumentUpload> SetCollection(DocumentUpload documentUpload, FileUploadDTO fileUploadDTO)
        {
            var result = documentUpload;

            Collection? collection = new Collection();

            collection = (!String.IsNullOrEmpty(fileUploadDTO.newCollectionName))
                ? new Collection()
                {
                    Name = fileUploadDTO.newCollectionName,
                }
                : _context.Collections.FirstOrDefault(x => x.Id == fileUploadDTO.collectionId);

            result.Collection = collection;

            return result;

        }

        private async Task<DocumentUpload> SetCategory(DocumentUpload documentUpload, FileUploadDTO fileUploadDTO)
        {
            var result = documentUpload;

            Category? category = new Category();

            category = (!String.IsNullOrEmpty(fileUploadDTO.newCategoryName))
                ? new Category()
                {
                    Name = fileUploadDTO.newCategoryName,
                }
                : _context.Categories.FirstOrDefault(x => x.Id == fileUploadDTO.categoryId);

            result.Category = category;

            return result;
        }


        private async Task<List<Term>> GetTerms(FileUploadDTO fileUploadDTO)
        {
            List<Term> termsList = new List<Term>();


            if (fileUploadDTO.termIds != null
                    && fileUploadDTO.termIds.Count > 0)
            {
                foreach (var termId in fileUploadDTO.termIds)
                {
                    int termIdValue = -1;
                    Int32.TryParse(termId, out termIdValue);
                    if (termIdValue >= 0)
                    {
                        termIdValue++;
                        termsList.Add(_context.Terms.FirstOrDefault(x => x.Id.Equals(termIdValue)));
                    }
                }
            }

            if (!String.IsNullOrEmpty(fileUploadDTO.newTermNames))
            {
                fileUploadDTO.newTermNames = fileUploadDTO.newTermNames.TrimStart('[').TrimEnd(']');
                var newTerms = fileUploadDTO.newTermNames.Split(",");
                foreach (var newTerm in newTerms)
                {
                    var newTermEntity = new Term()
                    {
                        Name = newTerm.TrimStart('"').TrimEnd('"')
                    };
                    termsList.Add(newTermEntity);
                    _context.Terms.Add(newTermEntity);
                    await _context.SaveChangesAsync();
                }
            }

            return termsList;

        }

        #endregion
        
        #region Summary

        public async Task<string> GetTextFileSummaryFromPath(LocalFileTypes localFileTypes)
        {
            var result = "";
            var filePath = new DirectoryInfo(".").Parent.FullName + @"\SampleData\";

            switch (localFileTypes)
            {
                case LocalFileTypes.SportsHistory:
                    filePath += "DirtyBird-Wikipedia.html";
                    break;
                case LocalFileTypes.Top5Movies:
                    filePath += "top5movies.txt";
                    break;
                case LocalFileTypes.TheOdyssey:
                    filePath += "pg1727_TheOdyssey.txt";
                    break;
                default:
                    filePath = "DEFAULT - NOT FOUND";
                    break;
            }

            var fileInfo = new FileInfo(filePath);

            result = await this.GetTextFileSummaryFromPath(fileInfo);


            return result;
        }


        public async Task<string> GetTextFileSummaryFromPath(FileInfo fi)
        {
            var result = "";
            try
            {
                    var fileBytes = File.ReadAllBytes(fi.FullName);
                    var base64Data = Convert.ToBase64String(fileBytes);
                    var summary = await GetTextSummary(base64Data);
                    result = summary;
                
            }
            catch (Exception ex)
            {
                return ex.Message + " " + ex.StackTrace;
            }

            return result;
        }



        public async Task<string> GetTextSummary(string base64Data, bool isPDF = false)
        {
            var result = "";
            var fileText = "";

            //if (!isPDF)
            //{
                fileText = _textManager.GetTextFileContent(base64Data);
            //}
            //else
            //{
            //    fileText = await _pdfManager.GetContent(base64Data);
            //}

            try
            {
                var chatHistory = new ChatHistory();
                var prompt = Prompts.SummarizeText;

                chatHistory.AddUserMessage([
                        new TextContent(prompt),
                    ]);

                chatHistory.AddUserMessage(prompt);

                var pieces = _textManager.GetChunks(fileText);

                for (int i = 0; i < pieces.Count(); i++) {
                    chatHistory.AddUserMessage(String.Format(" Text Section[{0}] - {1} - End Text Section[{0}] ", i, pieces[i]));
                }
                if (pieces.Count == 0)
                {
                    chatHistory.AddUserMessage("Text to summarize: " + fileText + " --- end of text to summarize");
                }

                var reply = await _chatCompletionService.GetChatMessageContentAsync(chatHistory);

                result = reply.Content;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
                Console.WriteLine("ERROR: " + ex.StackTrace);

            }

            return result;
        }

        #endregion



    }
}
