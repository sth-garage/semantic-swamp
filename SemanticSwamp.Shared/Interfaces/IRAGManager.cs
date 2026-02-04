using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.Models.RAG;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticSwamp.Shared.Interfaces
{
    public interface IRAGManager
    {
        Task UploadToRAG(DocumentUpload documentUpload, string overrideText = null);

        Task<List<DocumentUploadRAGEntry>> Search(string promptOrQuestion);
    }
}
