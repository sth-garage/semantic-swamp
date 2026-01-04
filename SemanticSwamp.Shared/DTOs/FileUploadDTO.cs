using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticSwamp.Shared.DTOs
{

    public class FileUploadDTO
    {
        public IFormFile file { get; set; }
        public int? categoryId { get; set; } = 1;
        public int? collectionId { get; set; } = 1;
        public string? newCollectionName { get; set; } = "";
        public string? newCategoryName { get; set; } = "";
        public string newTermNames { get; set; } = "";
        public List<string> termIds { get; set; } = new List<string>();
    }

    public class UploadLocalPayload
    {
        public string LocalFileName { get; set; }
    }

    public class DownloadLocalPayload
    {
        public int documentUploadId { get; set; }
    }
}


