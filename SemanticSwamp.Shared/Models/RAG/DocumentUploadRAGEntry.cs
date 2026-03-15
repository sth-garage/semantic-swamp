using Microsoft.Extensions.VectorData;

namespace SemanticSwamp.Shared.Models.RAG
{
    /// <summary>
    /// Represents a single chunk of a document that has been embedded and stored as a vector
    /// point in Qdrant. One DocumentUpload typically produces multiple RAG entries—one per
    /// text chunk (split by RAGManager.GetChunks using SK's TextChunker).
    ///
    /// The VectorStore* attributes from Microsoft.Extensions.VectorData instruct the SK
    /// Qdrant connector how to map each property to a Qdrant payload field or vector:
    ///   [VectorStoreKey]      → the ulong point ID in Qdrant
    ///   [VectorStoreData]     → stored as a Qdrant payload field (filterable when IsIndexed = true)
    ///   [VectorStoreVector]   → the dense float vector used for similarity search (384 dimensions)
    ///
    /// During search (RAGManager.Search), the caller's question is embedded into the same
    /// 384-dimensional space and the top-30 nearest neighbours are returned. The CollectionId
    /// filter is optionally applied first to scope results to the most relevant collection.
    /// </summary>
    public class DocumentUploadRAGEntry
    {
        /// <summary>
        /// Unique point ID in Qdrant. Assigned from IdTracker.LastIdUsed to ensure monotonic
        /// uniqueness across all upsert batches without querying Qdrant for the current max.
        /// </summary>
        [VectorStoreKey]
        public ulong Id { get; set; }

        /// <summary>FK to the originating DocumentUpload row, allowing reverse-lookup to the full record.</summary>
        [VectorStoreData(IsIndexed = true)]
        public int DocumentUploadId { get; set; }

        /// <summary>Stored for metadata filtering — allows scoping searches to a specific category.</summary>
        [VectorStoreData(IsIndexed = true)]
        public int CategoryId { get; set; }

        /// <summary>
        /// Stored for metadata filtering. RAGManager.GetCollectionIdFromQuestion uses the AI
        /// to identify the best-matching collection, then filters vectors by this field.
        /// </summary>
        [VectorStoreData(IsIndexed = true)]
        public int CollectionId { get; set; }

        /// <summary>Original filename, stored for attribution in search results.</summary>
        [VectorStoreData(IsIndexed = true)]
        public string FileName { get; set; }

        /// <summary>IDs of the Term tags applied to the parent document, enabling future term-scoped search.</summary>
        [VectorStoreData(IsIndexed = true)]
        public List<int> Terms { get; set; } = new List<int>();

        /// <summary>
        /// The raw text of this chunk. Marked IsFullTextIndexed so Qdrant can also perform
        /// keyword search on it in addition to vector similarity search.
        /// </summary>
        [VectorStoreData(IsFullTextIndexed = true)]
        public string Text { get; set; }

        /// <summary>Zero-based chunk index within the parent document (chunk 0, 1, 2…).</summary>
        [VectorStoreData]
        public int Index { get; set; }

        /// <summary>Timestamp when this entry was created, formatted as "yyyyMMdd_HHmmss".</summary>
        [VectorStoreData]
        public string CreatedOn { get; set; }

        /// <summary>
        /// The dense vector embedding of the Text chunk, produced by ITextEmbeddingGenerationService.
        /// 384 dimensions matches the output of the local SmartComponents embedding model.
        /// </summary>
        [VectorStoreVector(384)]
        public ReadOnlyMemory<float>? TextEmbedding { get; set; }
    }
}
