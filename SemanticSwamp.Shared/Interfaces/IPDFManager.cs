using Microsoft.SemanticKernel.ChatCompletion;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticSwamp.Shared.Interfaces
{
    public interface IPDFManager
    {
        Task<string> GetContent(List<PDFText> pdfTexts);

        List<PDFText> GetPDFText(DocumentUpload documentUpload);

        List<PDFText> GetPDFText(string base64Data);

        List<PDFText> GetPDFText(byte[] bytes);

    }
}
