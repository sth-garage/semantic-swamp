namespace SemanticKernel.MCP.Tools;

public sealed class DocumentUploadInfoDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
    public DateTime CreatedOn { get; set; }
    public bool IsActive { get; set; }
    public bool HasBeenProcessed { get; set; }
    public string? Summary { get; set; }
    public int CollectionId { get; set; }
    public int CategoryId { get; set; }
}

public sealed class RagUploadResultDto
{
    public bool Success { get; set; }
}

public sealed class RagSearchResultDto
{
    public ulong Id { get; set; }
    public int DocumentUploadId { get; set; }
    public int CategoryId { get; set; }
    public int CollectionId { get; set; }
    public string FileName { get; set; } = "";
    public List<int> Terms { get; set; } = new();
    public string Text { get; set; } = "";
    public int Index { get; set; }
    public string? CreatedOn { get; set; }
}
