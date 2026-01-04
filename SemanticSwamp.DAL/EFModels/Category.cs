using System;
using System.Collections.Generic;

namespace SemanticSwamp.DAL.EFModels;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<DocumentUpload> DocumentUploads { get; set; } = new List<DocumentUpload>();
}
