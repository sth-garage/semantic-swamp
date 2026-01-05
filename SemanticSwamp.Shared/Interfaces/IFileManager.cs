using Microsoft.AspNetCore.Http;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Text;
using static SemanticSwamp.Shared.Enums;

namespace SemanticSwamp.Shared.Interfaces
{
    public interface IFileManager
    {
        Task<DocumentUpload> ProcessUpload(FileUploadDTO fileUploadDTO);
        Task<string> GetTextSummary(string base64Data);
        Task<string> GetBase64DataFromFile(IFormFile file);
        Task<string> GetTextFileSummaryFromPath(LocalFileTypes localFileTypes);

        Task<string> GetTextFileSummaryFromText(string text);
    }
}
