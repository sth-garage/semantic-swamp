using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.Models.RAG;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticSwamp.Shared.Interfaces
{
    public interface IRAGManager
    {
        Task Upload(DocumentUpload documentUpload);

        Task<List<DocumentUploadRAGEntry>> Search(string promptOrQuestion);
    }
}
