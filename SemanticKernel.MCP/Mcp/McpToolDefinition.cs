using System.Text.Json.Serialization;

namespace SemanticKernel.MCP.Mcp;

public sealed class McpToolDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new { type = "object", properties = new { } };
}
