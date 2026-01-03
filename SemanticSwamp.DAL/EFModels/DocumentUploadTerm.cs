using System;
using System.Collections.Generic;

namespace SemanticSwamp.DAL.EFModels;

public partial class DocumentUploadTerm
{
    public int Id { get; set; }

    public int TermId { get; set; }

    public int DocumentUploadId { get; set; }

    public virtual Term Term { get; set; } = null!;
}
