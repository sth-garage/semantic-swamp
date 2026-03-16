using SemanticSwamp.MCPBlazor.Models;
using System.Diagnostics;

namespace SemanticSwamp.MCPBlazor.Services;

public class ChatService
{
    private readonly IMcpClient _mcpClient;

    public List<UiChatMessage> Messages { get; } = new();

    public bool IsWaiting { get; private set; }
    public bool IsReady { get; private set; }

    public string Status { get; private set; } = "Connecting…";
    public string StatusKind { get; private set; } = "connecting";

    public event Action? OnStateChanged;

    private string? _sessionId;

    public ChatService(IMcpClient mcpClient)
    {
        _mcpClient = mcpClient;
    }

    public async Task IntroduceAsync()
    {
        Status = "Meeting Semantigator…";
        StatusKind = "connecting";
        OnStateChanged?.Invoke();

        await SendCoreAsync("Introduce yourself", addUserMessage: false);

        IsReady = true;
        Status = "Ready";
        StatusKind = "ready";
        OnStateChanged?.Invoke();
    }

    public async Task SendMessageAsync(string text)
    {
        if (IsWaiting || !IsReady) return;

        Messages.Add(new UiChatMessage
        {
            Role = "Client",
            Content = System.Web.HttpUtility.HtmlEncode(text),
            Timestamp = DateTime.Now
        });

        await SendCoreAsync(text, addUserMessage: true);
    }

    private async Task SendCoreAsync(string text, bool addUserMessage)
    {
        IsWaiting = true;
        Status = "Swishing through reeds…";
        StatusKind = "thinking";
        OnStateChanged?.Invoke();

        var sw = Stopwatch.StartNew();

        try
        {
            var payload = new
            {
                message = addUserMessage ? (text + " /nothink") : text,
                sessionId = _sessionId,
                reset = false
            };

            var result = await _mcpClient.CallToolAsync<McpChatResponse>("chat", payload);
            _sessionId = result.SessionId;

            sw.Stop();

            var content = CleanResponse(result.Content ?? "");

            Messages.Add(new UiChatMessage
            {
                Role = "Assistant",
                Content = content,
                LatencySeconds = result.LatencySeconds > 0 ? result.LatencySeconds : sw.Elapsed.TotalSeconds,
                Timestamp = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            Messages.Add(new UiChatMessage
            {
                Role = "System",
                Content = $"<em>An error occurred: {System.Web.HttpUtility.HtmlEncode(ex.Message)}</em>"
            });
        }
        finally
        {
            IsWaiting = false;
            Status = IsReady ? "Ready" : "Initializing…";
            StatusKind = IsReady ? "ready" : "connecting";
            OnStateChanged?.Invoke();
        }
    }

    private static string CleanResponse(string content)
    {
        var divTagIndex = content.IndexOf("<div>");
        if (divTagIndex > -1)
            content = content[divTagIndex..];

        return content
            .Replace("```html", "")
            .Replace("```", "")
            .Replace("<|channel|>final <|constrain|>html<|message|>", "")
            .Replace("<|channel|>final <|constrain|>", "")
            .Replace("div<|message|>", "")
            .Replace("<|message|>", "")
            .Replace("commentary", "");
    }

    private sealed class McpChatResponse
    {
        public string SessionId { get; set; } = "";
        public string Content { get; set; } = "";
        public double LatencySeconds { get; set; }
    }
}
