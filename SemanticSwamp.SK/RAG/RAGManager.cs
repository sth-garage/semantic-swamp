using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using Elastic.Clients.Elasticsearch;
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
using SemanticSwamp.Shared.Prompts;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

#pragma warning disable 

namespace SemanticSwamp.SK.RAG
{
    public class RAGManager : IRAGManager
    {
        private ITextManager _textManager;
        //private Kernel _kernel;
        private ITextEmbeddingGenerationService _textEmbeddingGenerationService;
        private string _ragCollectionName = "DocumentUpload";
        private SemanticSwampDBContext _context;
        private IChatCompletionService _chatCompletionService;


        public RAGManager(ITextManager textManager, 
            ITextEmbeddingGenerationService textEmbeddingGenerationService, 
            SemanticSwampDBContext context, 
            IChatCompletionService chatCompletionService) 
        { 
            _textManager = textManager;
            _chatCompletionService = chatCompletionService;
            _textEmbeddingGenerationService = textEmbeddingGenerationService;
            _context = context;
        }

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



        public async Task UploadToRAG(DocumentUpload documentUpload, string overrideText = null)
        {
            var vectorStore = new QdrantVectorStore(new QdrantClient("localhost"), ownsClient: true);
            
            var collection = new QdrantCollection<ulong, DocumentUploadRAGEntry>(
                new QdrantClient("localhost"),
                _ragCollectionName,
                ownsClient: true);


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

        public async Task<List<DocumentUploadRAGEntry>> Search(string promptOrQuestion)
        {
            var vectorStore = new QdrantVectorStore(new QdrantClient("localhost"), ownsClient: true);

            var collection = new QdrantCollection<ulong, DocumentUploadRAGEntry>(
                new QdrantClient("localhost"),
                _ragCollectionName,
                ownsClient: true);

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
