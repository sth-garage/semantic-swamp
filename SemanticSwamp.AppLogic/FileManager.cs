using Elastic.Clients.Elasticsearch.Aggregations;
using Microsoft.AspNetCore.Http;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.DTOs;
using SemanticSwamp.Shared.Interfaces;

namespace SemanticSwamp.AppLogic
{
    public class FileManager : IFileManager
    {
        private SemanticSwampDBContext _context;

        public FileManager(SemanticSwampDBContext context)
        {
            _context = context;
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
            //var inputTerms = fileUploadDTO.newTermNames ?? new List<string>();

            //var inputTermsSplit = fileUploadDTO.newTermNames.Split
            await _context.SaveChangesAsync();


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
                        Name = newTerm
                    };
                    termsList.Add(newTermEntity);
                    _context.Terms.Add(newTermEntity);
                    await _context.SaveChangesAsync();
                }
            }

            //await _context.SaveChangesAsync();

            //foreach (var term in termsList)
            //{
            //    _context.DocumentUploadTerms.Add(new DocumentUploadTerm
            //    {
            //        TermId = term.Id,
            //        DocumentUploadId = result.Id
            //    });

            //}

            return termsList;

        }

        private async Task<DocumentUpload> AddFileMetaData(DocumentUpload documentUpload, FileUploadDTO fileUploadDTO)
        {
            var result = documentUpload;

            result.Base64Data = await GetBase64DataFromFile(fileUploadDTO.file);
            result.FileName = fileUploadDTO.file.FileName;

            return result;
        }



        private async Task<string> GetBase64DataFromFile(IFormFile file)
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
