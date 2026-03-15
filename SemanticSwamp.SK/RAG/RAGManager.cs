using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
using Qdrant.Client;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.Interfaces;
using SemanticSwamp.Shared.Models.RAG;

#pragma warning disable 

namespace SemanticSwamp.SK.RAG
{
    /// <summary>
    /// Manages Retrieval-Augmented Generation (RAG) operations against a Qdrant vector database.
    /// <para>
    /// <b>Upload flow (<see cref="UploadToRAG"/>):</b>
    /// Text content is split into semantic paragraphs using Semantic Kernel's <c>TextChunker</c>
    /// (lines of up to 128 tokens, grouped into paragraphs of up to 1,024 tokens). Each paragraph
    /// is embedded with <c>ITextEmbeddingGenerationService</c> and upserted as a
    /// <see cref="DocumentUploadRAGEntry"/> in the Qdrant collection. A shared <c>IdTracker</c>
    /// SQL row provides monotonically increasing point IDs so they remain unique across restarts.
    /// </para>
    /// <para>
    /// <b>Search flow (<see cref="Search"/>):</b>
    /// The user's question is first sent to the AI to determine which <c>Collection</c> best
    /// matches the topic. If a collection is identified, the vector search is filtered to that
    /// collection only (reducing noise). The top 30 most similar chunks are returned.
    /// </para>
    /// </summary>
    public class RAGManager : IRAGManager
    {
        private ITextManager _textManager;
        private ITextEmbeddingGenerationService _textEmbeddingGenerationService;

        // The name of the Qdrant collection that stores all document chunk vectors.
        private string _ragCollectionName = "DocumentUpload";
        private SemanticSwampDBContext _context;
        private IChatCompletionService _chatCompletionService;
        private QdrantClient _qdrantClient;


        /// <summary>
        /// Initialises <see cref="RAGManager"/> with all services injected by the DI container.
        /// </summary>
        /// <param name="textManager">Decodes Base64 file content to plain text for chunking.</param>
        /// <param name="textEmbeddingGenerationService">Generates 384-dim dense vector embeddings for text chunks and queries.</param>
        /// <param name="context">EF Core context used to read the <c>IdTracker</c> row and collection names.</param>
        /// <param name="chatCompletionService">AI service used to identify the best-matching collection during search.</param>
        /// <param name="qdrantClient">Qdrant client pointing to the local vector store instance.</param>
        public RAGManager(ITextManager textManager, 
            ITextEmbeddingGenerationService textEmbeddingGenerationService, 
            SemanticSwampDBContext context, 
            IChatCompletionService chatCompletionService,
            QdrantClient qdrantClient) 
        {
            _textManager = textManager;
            _chatCompletionService = chatCompletionService;
            _textEmbeddingGenerationService = textEmbeddingGenerationService;
            _context = context;
            _qdrantClient = qdrantClient;
        }

        /// <summary>
        /// Splits a plain-text document into semantic paragraphs suitable for vector indexing.
        /// Uses Semantic Kernel's <c>TextChunker</c> in a two-pass approach:
        /// <list type="number">
        ///   <item>Split into lines of up to 128 tokens to respect sentence boundaries.</item>
        ///   <item>Group lines into paragraphs of up to 1,024 tokens to form coherent retrieval units.</item>
        /// </list>
        /// Smaller chunks improve retrieval precision; larger chunks provide more context per result.
        /// The 128/1024 token settings balance these concerns for typical document content.
        /// </summary>
        /// <param name="value">The full plain-text document content.</param>
        /// <returns>A list of paragraph-sized text chunks ready for embedding and upsert.</returns>
        public List<string> GetChunks(string value)
        {
            List<string> paragraphs =
                TextChunker.SplitPlainTextParagraphs(
                    TextChunker.SplitPlainTextLines(
                        value,
                128),
            1024);

            return paragraphs;
        }



        /// <summary>
        /// Splits the document into semantic chunks, generates a vector embedding for each chunk,
        /// and upserts every chunk as a <see cref="DocumentUploadRAGEntry"/> into the Qdrant collection.
        /// <para>
        /// The <c>IdTracker</c> table in SQL Server holds the last-used Qdrant point ID so that IDs
        /// remain unique across application restarts (Qdrant requires globally unique ulong IDs per collection).
        /// </para>
        /// </summary>
        /// <param name="documentUpload">The persisted document entity providing metadata (ID, category, collection, terms, filename).</param>
        /// <param name="overrideText">
        /// When provided (e.g., for PDFs), this text is used instead of decoding <paramref name="documentUpload"/>.Base64Data.
        /// PDF content is pre-extracted and AI-corrected by <c>PDFManager</c> before being passed here.
        /// </param>
        public async Task UploadToRAG(DocumentUpload documentUpload, string overrideText = null)
        {
            var collection = new QdrantCollection<ulong, DocumentUploadRAGEntry>(
                _qdrantClient,
                _ragCollectionName,
                ownsClient: false);


            await collection.EnsureCollectionExistsAsync();

            var content = _textManager.GetTextFileContent(documentUpload.Base64Data);

            if (!String.IsNullOrEmpty(overrideText))
            {
                content = overrideText;
            }

            var pieces = GetChunks(content);

            var idTracker = _context.IdTrackers.First();

            ulong idValue = (ulong) idTracker.LastIdUsed;
            

            for (var i = 0; i < pieces.Count; i++)
            {
                var current = pieces[i];

                var entry = new DocumentUploadRAGEntry
                {
                    DocumentUploadId = documentUpload.Id,
                    CategoryId = documentUpload.CategoryId,
                    CollectionId = documentUpload.CollectionId,
                    Terms = documentUpload.DocumentUploadTerms.Select(x => x.TermId).ToList(),
                    Text = current,
                    TextEmbedding = await _textEmbeddingGenerationService.GenerateEmbeddingAsync(current),
                    Index = i,
                    Id = idValue++,
                    CreatedOn = DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                    FileName = documentUpload.FileName,
                };

                await collection.UpsertAsync(entry);
            }
            idTracker.LastIdUsed = (int) idValue;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Performs a semantic similarity search against the Qdrant vector store.
        /// <para>
        /// Before running the vector query, the AI is asked which of the existing <c>Collections</c>
        /// best matches the question. If the AI's answer matches a known collection name exactly,
        /// the search is filtered to that collection to reduce irrelevant results. If no match is
        /// found, the search runs across all collections.
        /// Returns up to 30 of the most semantically similar document chunks.
        /// </para>
        /// </summary>
        /// <param name="promptOrQuestion">The user's natural-language question or chat message.</param>
        /// <returns>A ranked list of the most relevant <see cref="DocumentUploadRAGEntry"/> chunks.</returns>
        public async Task<List<DocumentUploadRAGEntry>> Search(string promptOrQuestion)
        {
            var collection = new QdrantCollection<ulong, DocumentUploadRAGEntry>(
                _qdrantClient,
                _ragCollectionName,
                ownsClient: false);

            await collection.EnsureCollectionExistsAsync();
            ReadOnlyMemory<float> searchEmbedding = await _textEmbeddingGenerationService.GenerateEmbeddingAsync(promptOrQuestion);

            var result = new List<DocumentUploadRAGEntry>();

            var ragCollectionId = await GetCollectionIdFromQuestion(promptOrQuestion);


            var options = new VectorSearchOptions<DocumentUploadRAGEntry>();

            if (ragCollectionId > -1)
            {
                options.Filter = (x => x.CollectionId == ragCollectionId);
            }

            await collection.SearchAsync(searchEmbedding, top: 30, options)
                .ForEachAsync(x =>
                {
                    result.Add(x.Record);
                });

            return result;
        }

        /// <summary>
        /// Asks the AI to identify which stored <c>Collection</c> is most relevant to the given question.
        /// The model is given a comma-separated list of all collection names and asked to pick the best fit.
        /// The response is cleaned of markdown formatting and matched against the database.
        /// </summary>
        /// <param name="question">The user's natural-language question.</param>
        /// <returns>
        /// The ID of the best-matching <see cref="Collection"/>, or <c>-1</c> if no match is found
        /// (which causes <see cref="Search"/> to run without a collection filter).
        /// </returns>
        private async Task<int> GetCollectionIdFromQuestion(string question)
        {
            var result = -1;
            var chatHistory = new ChatHistory();
            var existing = String.Join(',', _context.Collections.Select(x => x.Name).ToList());
            var prompt = "Here are a list of collections : [" + existing + "] - which best captures this question: " + question;


            chatHistory.AddUserMessage([
                    new TextContent(prompt),
                    ]);

            chatHistory.AddUserMessage(prompt);
            var reply = await _chatCompletionService.GetChatMessageContentAsync(chatHistory);

            var content = reply.Content.Replace("*", "").Replace("[", "").Replace("]", "");

            var ragCollection = _context.Collections.FirstOrDefault(x => x.Name == content);
            if (ragCollection != null)
            {
                result = ragCollection.Id;
            }

            return result;
        }
    }




}
