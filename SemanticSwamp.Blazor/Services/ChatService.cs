using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticSwamp.Blazor.Models;
using SemanticSwamp.Shared.Prompts;
using System.Diagnostics;

#pragma warning disable SKEXP0010

namespace SemanticSwamp.Blazor.Services;

public class ChatService
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly Kernel _kernel;
    private readonly ChatHistory _chatHistory;

    public List<UiChatMessage> Messages { get; } = new();
    public bool IsWaiting { get; private set; }
    public bool IsReady { get; private set; }
    public string Status { get; private set; } = "Connecting…";
    public string StatusKind { get; private set; } = "connecting";

    public event Action? OnStateChanged;

    public ChatService(IChatCompletionService chatCompletionService, Kernel kernel)
    {
        _chatCompletionService = chatCompletionService;
        _kernel = kernel;
        _chatHistory = new ChatHistory();
        _chatHistory.AddSystemMessage(Prompts.TempSystemPrompt);
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
            if (addUserMessage)
                _chatHistory.AddUserMessage(text + " /nothink");
            else
                _chatHistory.AddUserMessage(text);

            var settings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            var result = await _chatCompletionService.GetChatMessageContentAsync(
                _chatHistory, settings, _kernel);

            sw.Stop();

            var content = CleanResponse(result.Content ?? "");
            _chatHistory.AddAssistantMessage(content);

            Messages.Add(new UiChatMessage
            {
                Role = "Assistant",
                Content = content,
                LatencySeconds = sw.Elapsed.TotalSeconds,
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
}
