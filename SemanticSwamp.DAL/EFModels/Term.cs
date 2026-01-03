using System;
using System.Collections.Generic;

namespace SemanticSwamp.DAL.EFModels;

public partial class Term
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<DocumentUploadTerm> DocumentUploadTerms { get; set; } = new List<DocumentUploadTerm>();
}
