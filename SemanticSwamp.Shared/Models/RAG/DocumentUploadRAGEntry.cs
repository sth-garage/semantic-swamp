using Microsoft.Extensions.VectorData;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticSwamp.Shared.Models.RAG
{
    public class DocumentUploadRAGEntry
    {
        [VectorStoreKey]
        public Guid Id { get; set; }


        [VectorStoreData(IsIndexed = true)]
        public int DocumentUploadId { get; set; }

        [VectorStoreData(IsIndexed = true)]
        public int CategoryId { get; set; }

        [VectorStoreData(IsIndexed = true)]
        public int CollectionId { get; set; }

        [VectorStoreData(IsIndexed = true)]
        public string FileName { get; set; }

        [VectorStoreData(IsIndexed = true)]
        public List<int> Terms { get; set; } = new List<int>();

        [VectorStoreData(IsFullTextIndexed = true)]
        public string Text { get; set; }

        [VectorStoreData]
        public int Index { get; set; }

        [VectorStoreData]
        public string CreatedOn { get; set; }

        [VectorStoreVector(384)]
        public ReadOnlyMemory<float>? TextEmbedding { get; set; }
    }
}
