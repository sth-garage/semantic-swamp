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

#pragma warning disable SKEXP0001 
namespace SemanticSwamp.AppLogic
{
    /// <summary>
    /// Orchestrates the complete document upload pipeline from raw file bytes through to
    /// a fully indexed, searchable vector entry in the Qdrant RAG store.
    /// <para>
    /// The high-level flow handled by <see cref="ProcessUpload"/> is:
    /// <list type="number">
    ///   <item>Parse and create any new <c>Term</c> entities from the DTO.</item>
    ///   <item>Extract file metadata (filename, Base64 content).</item>
    ///   <item>Resolve or create the target <c>Collection</c> and <c>Category</c>.</item>
    ///   <item>Persist the <see cref="DocumentUpload"/> record and its term links to SQL Server.</item>
    ///   <item>If the file is a PDF, extract its text via <c>PDFManager</c>.</item>
    ///   <item>Generate an AI summary via <c>IChatCompletionService</c>.</item>
    ///   <item>Upload chunked text embeddings to the Qdrant vector store via <c>RAGManager</c>.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class UploadManager : IFileManager
    {
        private SemanticSwampDBContext _context;
        private IChatCompletionService _chatCompletionService;
        private ITextManager _textManager;
        private IRAGManager _ragManager;
        private IPDFManager _pdfManager;

        /// <summary>
        /// Initialises <see cref="UploadManager"/> with all required services injected by the DI container.
        /// </summary>
        /// <param name="context">EF Core database context for reading/writing SQL Server entities.</param>
        /// <param name="chatCompletionService">Semantic Kernel chat completion service used for AI summarisation.</param>
        /// <param name="textManager">Utility for Base64 encoding/decoding and text chunking.</param>
        /// <param name="ragManager">Manages chunked vector upserts into the Qdrant collection.</param>
        /// <param name="pdfManager">Extracts and reconstructs text from PDF files using PdfPig + AI.</param>
        public UploadManager(SemanticSwampDBContext context, IChatCompletionService chatCompletionService, ITextManager textManager, IRAGManager ragManager, IPDFManager pdfManager)
        {
            _context = context;
            _chatCompletionService = chatCompletionService;
            _textManager = textManager;
            _ragManager = ragManager;
            _pdfManager = pdfManager;
        }

        /// <summary>
        /// End-to-end upload pipeline entry point.
        /// Saves the document record, links taxonomy entities (collection, category, terms),
        /// generates an AI summary, and indexes the document in the Qdrant RAG vector store.
        /// </summary>
        /// <param name="fileUploadDTO">DTO containing the uploaded file and all user-chosen metadata.</param>
        /// <returns>The fully populated and persisted <see cref="DocumentUpload"/> entity.</returns>
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

            var isPDF = result.FileName.ToLowerInvariant().EndsWith("pdf");

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
        /// <summary>
        /// Reads the uploaded file's bytes via <c>IFormFile</c> and stores them as a Base64 string
        /// on the entity, along with the original filename. Using Base64 allows arbitrary binary
        /// files to be stored safely in a SQL Server text column.
        /// </summary>
        private async Task<DocumentUpload> AddFileMetaData(DocumentUpload documentUpload, FileUploadDTO fileUploadDTO)
        {
            var result = documentUpload;

            result.Base64Data = await _textManager.GetBase64DataFromFile(fileUploadDTO.file);
            result.FileName = fileUploadDTO.file.FileName;

            return result;
        }

        /// <summary>
        /// Creates <see cref="DocumentUploadTerm"/> join-table rows linking each resolved <see cref="Term"/>
        /// to the saved <see cref="DocumentUpload"/>. This many-to-many relationship allows documents
        /// to be filtered by tag/term in the RAG search pipeline.
        /// </summary>
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

        /// <summary>
        /// Resolves the <see cref="Collection"/> for this upload.
        /// If the DTO supplies a new collection name a fresh entity is created (EF will INSERT it);
        /// otherwise the existing collection is looked up by its ID.
        /// Collections group documents by broad topic area and are used to scope RAG searches.
        /// </summary>
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

        /// <summary>
        /// Resolves the <see cref="Category"/> for this upload.
        /// Mirrors the pattern in <see cref="SetCollection"/>: creates a new entity if a new name
        /// is provided, otherwise fetches the existing one by ID.
        /// Categories provide a finer-grained classification within a collection.
        /// </summary>
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


        /// <summary>
        /// Resolves the list of <see cref="Term"/> entities to associate with this upload.
        /// Handles two sources:
        /// <list type="bullet">
        ///   <item>Existing terms: IDs are provided in <c>fileUploadDTO.termIds</c>.
        ///         Note: the ID is incremented by 1 before lookup (<c>termIdValue++</c>) to
        ///         account for the zero-based index offset sent from the UI.</item>
        ///   <item>New terms: a JSON-array string in <c>fileUploadDTO.newTermNames</c> is parsed,
        ///         each name is trimmed of brackets/quotes, and a new <see cref="Term"/> entity
        ///         is inserted into the database immediately.</item>
        /// </list>
        /// </summary>
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

        /// <summary>
        /// Sends document content to the AI chat completion service and returns a plain-text summary.
        /// <para>
        /// The text is first decoded from Base64, then split into 10,000-character chunks via
        /// <see cref="ITextManager.GetChunks"/>. Each chunk is added as a separate user message in the
        /// <see cref="ChatHistory"/> so that large documents do not overflow the model's context window.
        /// If the text is short enough to fit in one chunk, it is sent as a single message.
        /// </para>
        /// </summary>
        /// <param name="base64Data">Base64-encoded document text (PDF-extracted text is re-encoded before being passed here).</param>
        /// <param name="isPDF">Currently unused; reserved for potential future prompt variation for PDF content.</param>
        /// <returns>The AI-generated summary string, or an empty string if an exception occurs.</returns>
        public async Task<string> GetTextSummary(string base64Data, bool isPDF = false)
        {
            var result = "";
            var fileText = "";

            fileText = _textManager.GetTextFileContent(base64Data);

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
