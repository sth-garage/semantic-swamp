using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticSwamp.Shared.Interfaces
{
    public interface IFileManager
    {
        Task<DocumentUpload> ProcessUpload(FileUploadDTO fileUploadDTO);
    }
}
