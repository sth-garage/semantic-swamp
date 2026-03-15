namespace SemanticSwamp.Blazor.Models;

/// <summary>
/// Represents a single message displayed in the chat UI.
/// 
/// Each message has a role that controls how it is visually rendered:
///   - "Client"    → right-aligned bubble with a green "YOU" avatar; HTML is encoded before storage
///   - "Assistant" → left-aligned bubble with the Semantigator avatar; includes a latency badge
///   - "System"    → centred, italicised bubble used for status notices and error messages
/// 
/// The Id is used by Home.razor to identify the most recent assistant message so it can
/// apply a highlight ring and trigger the splash animation when a new response arrives.
/// </summary>
public class UiChatMessage
{
    /// <summary>
    /// Unique identifier for this message. Generated automatically on creation.
    /// Used by the rendering loop to detect and highlight the latest assistant reply.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Who sent the message. Valid values: "Client", "Assistant", "System".
    /// Drives CSS class selection and avatar rendering in the chat lake.
    /// </summary>
    public string Role { get; set; } = "System";

    /// <summary>
    /// The message body rendered as raw HTML via @((MarkupString)msg.Content).
    /// Client messages are HTML-encoded before storage to prevent injection.
    /// Assistant messages arrive pre-formatted as HTML from the AI (cleaned by ChatService).
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    /// How many seconds the AI took to respond, measured by a Stopwatch in ChatService.
    /// Null for client and system messages. Displayed as a clickable latency badge on
    /// assistant bubbles that opens the message detail modal.
    /// </summary>
    public double? LatencySeconds { get; set; }

    /// <summary>
    /// Wall-clock time when the message was created. Displayed in the message detail modal.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
