using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Text;
using Qdrant.Client;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.Interfaces;
using SemanticSwamp.Shared.Models.RAG;
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


        public RAGManager(ITextManager textManager, ITextEmbeddingGenerationService textEmbeddingGenerationService) 
        { 
            _textManager = textManager;
            //_kernel = kernel;
            _textEmbeddingGenerationService = textEmbeddingGenerationService;
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

        public async Task Upload(DocumentUpload documentUpload)
        {
            var vectorStore = new QdrantVectorStore(new QdrantClient("localhost"), ownsClient: true);
            //var textEmbed = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();

            var collection = new QdrantCollection<ulong, DocumentUploadRAGEntry>(
                new QdrantClient("localhost"),
                _ragCollectionName,
                ownsClient: true);

            await collection.EnsureCollectionExistsAsync();

            var content = _textManager.GetTextFileContent(documentUpload.Base64Data);
            var pieces = GetChunks(content);

            ulong idValue = 1;
            

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

            //var searchResult = new List<VectorSe>
            //var searchVector = (await _textEmbeddingGenerationService.GenerateEmbeddingAsync("What are some security improvements in .NET?"));
            var options = new VectorSearchOptions<DocumentUploadRAGEntry>
            {
                //Filter = (x => x.Index > 10)
            };
            await collection.SearchAsync(searchEmbedding, top: 30, options)
                .ForEachAsync(x =>
                {
                    //var blah = x.Record;
                    result.Add(x.Record);
                    //var sto = 1;
                });

            return result;
        }
    }




}
