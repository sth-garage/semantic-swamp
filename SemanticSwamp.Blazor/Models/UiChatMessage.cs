namespace SemanticSwamp.Blazor.Models;

public class UiChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Role { get; set; } = "System";
    public string Content { get; set; } = "";
    public double? LatencySeconds { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
