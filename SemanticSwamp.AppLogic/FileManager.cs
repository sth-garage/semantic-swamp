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

namespace SemanticSwamp.AppLogic
{
    public class FileManager : IFileManager
    {
        private SemanticSwampDBContext _context;
        private IChatCompletionService _chatCompletionService;

        public FileManager(SemanticSwampDBContext context, IChatCompletionService chatCompletionService)
        {
            _context = context;
            _chatCompletionService = chatCompletionService;

            
        }

        public async Task<DocumentUpload> ProcessUpload(FileUploadDTO fileUploadDTO)
        {
            var terms = await GetTerms(fileUploadDTO);
            await _context.SaveChangesAsync();

            var result = new DocumentUpload()
            {
                IsActive = true,
                CreatedOn = DateTime.UtcNow,
                HasBeenProcessed = false,
            };


            result = await AddFileMetaData(result, fileUploadDTO);
            result = await SetCollection(result, fileUploadDTO);
            result = await SetCategory(result, fileUploadDTO);

            _context.DocumentUploads.Add(result);
            await _context.SaveChangesAsync();

            await LinkTermsToDocumentUpload(terms, result);
            await _context.SaveChangesAsync();

            var summary = await GetTextSummary(result.Base64Data);
            result.Summary = summary;
            await _context.SaveChangesAsync();


            return result;
        }

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

        public async Task<string> GetTextFileSummaryFromText(string text)
        {
            var result = "";

            var bytes = Encoding.UTF8.GetBytes(text);

            var base64Data = Convert.ToBase64String(bytes);

            var summary = await GetTextSummary(base64Data);

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

        public async Task<string> GetTextFileSummaryFromPath(string path)
        {
            var result = "";
            try
            {
                FileInfo fi = new FileInfo(path);


                return await GetTextFileSummaryFromPath(fi);
            }
            catch(Exception ex)
            {
                return ex.Message + " " + ex.StackTrace;
            }

            return result;
        }

        public async Task<string> GetTextSummary(string base64Data)
        {
            var result = "";

            var fileText = await GetTextFileContent(base64Data);

            try
            {
                var chatHistory = new ChatHistory();
                var prompt = Prompts.SummarizeText;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                chatHistory.AddUserMessage([
                        new TextContent(prompt),
                        //new TextContent(fileText),
                    ]);

                chatHistory.AddUserMessage(prompt);

                var pieces = Split(fileText, 10000).ToList();
                for (int i = 0; i < pieces.Count(); i++) {
                    chatHistory.AddDeveloperMessage(String.Format("Text Section[{0}] - {1}", i, pieces[i]));
                }



#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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


        static IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
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
            //var result = documentUpload;
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

        public async Task<DocumentUpload> AddFileMetaData(DocumentUpload documentUpload, FileUploadDTO fileUploadDTO)
        {
            var result = documentUpload;

            result.Base64Data = await GetBase64DataFromFile(fileUploadDTO.file);
            result.FileName = fileUploadDTO.file.FileName;

            return result;
        }

        private async Task<string> GetTextFileContent(string base64Data)
        {
            var result = "";

            var bytes = Convert.FromBase64String(base64Data);
            result = System.Text.Encoding.UTF8.GetString(bytes);
            return result;
        }

        public async Task<string> GetBase64DataFromFile(IFormFile file)
        {
            var bytes = await GetBytesFromIFormFileAsync(file);

            return Convert.ToBase64String(bytes);
        }

        private async Task<byte[]> GetBytesFromIFormFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                return stream.ToArray();
            }
        }
    }
}
