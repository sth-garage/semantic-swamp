using System;
using System.Collections.Generic;

namespace SemanticSwamp.DAL.EFModels;

public partial class DocumentUpload
{
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public string? Base64Data { get; set; }

    public bool IsActive { get; set; }

    public bool HasBeenProcessed { get; set; }
}
