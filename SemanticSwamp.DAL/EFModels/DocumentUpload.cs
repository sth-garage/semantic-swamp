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

    public int CollectionId { get; set; }

    public int CategoryId { get; set; }

    public string? Summary { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual Collection Collection { get; set; } = null!;

    public virtual ICollection<DocumentUploadTerm> DocumentUploadTerms { get; set; } = new List<DocumentUploadTerm>();
}
