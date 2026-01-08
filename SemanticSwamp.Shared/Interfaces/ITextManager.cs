using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticSwamp.Shared.Interfaces
{
    public interface ITextManager
    {
        string GetTextFileContent(string base64Data);
        Task<string> GetBase64DataFromFile(IFormFile file);

        List<string> GetChunks(string text, int chunkSize = 10000);
    }
}
