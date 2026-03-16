using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticSwamp.Shared.Prompts;

#pragma warning disable SKEXP0010

namespace SemanticKernel.MCP.Chat;

public sealed class McpChatSessionManager
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly ConcurrentDictionary<string, ChatHistory> _sessions = new(StringComparer.Ordinal);

    public McpChatSessionManager(Kernel kernel, IChatCompletionService chatCompletionService)
    {
        _kernel = kernel;
        _chatCompletionService = chatCompletionService;
    }

    public async Task<McpChatResponseDto> SendAsync(
        string message,
        string? sessionId,
        bool reset,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty", nameof(message));
        }

        var id = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId;

        if (reset)
        {
            _sessions.TryRemove(id, out _);
        }

        var history = _sessions.GetOrAdd(id, _ => CreateNewHistory());

        history.AddUserMessage(message + " /nothink");

        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        var sw = Stopwatch.StartNew();
        var result = await _chatCompletionService.GetChatMessageContentAsync(history, settings, _kernel, cancellationToken);
        sw.Stop();

        var content = (result.Content ?? string.Empty).Trim();
        history.AddAssistantMessage(content);

        return new McpChatResponseDto
        {
            SessionId = id,
            Content = content,
            LatencySeconds = sw.Elapsed.TotalSeconds
        };
    }

    private static ChatHistory CreateNewHistory()
    {
        var h = new ChatHistory();
        h.AddSystemMessage(Prompts.TempSystemPrompt);
        return h;
    }
}

public sealed class McpChatResponseDto
{
    public string SessionId { get; set; } = "";
    public string Content { get; set; } = "";
    public double LatencySeconds { get; set; }
}
