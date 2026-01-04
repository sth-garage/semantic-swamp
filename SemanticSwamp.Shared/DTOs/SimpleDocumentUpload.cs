using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticSwamp.Shared.DTOs
{
    public class SimpleDocumentUpload
    {
        public string CollectionName { get; set; }

        public string CategoryName { get; set; }

        public string CreatedOn { get; set; }

        public bool IsActive { get; set; } = false;

        public string FileName { get; set; }

        public bool HasBeenProcessed { get; set; }
    }
}
