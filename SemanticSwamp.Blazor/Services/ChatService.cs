using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticSwamp.Blazor.Models;
using SemanticSwamp.Shared.Prompts;
using System.Diagnostics;

// Suppress the experimental-API warning for OpenAIPromptExecutionSettings.ToolCallBehavior,
// which is still marked [Experimental] in the Semantic Kernel SDK.
#pragma warning disable SKEXP0010

namespace SemanticSwamp.Blazor.Services;

/// <summary>
/// Scoped service that owns the Semantic Kernel chat session for a single browser connection.
///
/// Lifetime: one instance per Blazor Server SignalR circuit (i.e. per browser tab). This means
/// each user gets their own isolated ChatHistory and message list — no state leaks between users.
///
/// Responsibilities:
///   1. Maintain the SK ChatHistory so the AI has full conversational context.
///   2. Expose a UI-friendly Messages list (UiChatMessage) that the Razor component binds to.
///   3. Track status (IsWaiting, IsReady, Status, StatusKind) so the header status pill and
///      button disabled states always reflect the current state of the AI call.
///   4. Fire OnStateChanged so the Home.razor component knows when to call StateHasChanged().
/// </summary>
public class ChatService
{
    private readonly IChatCompletionService _chatCompletionService;
    private readonly Kernel _kernel;

    // SK ChatHistory accumulates every turn (system prompt + user + assistant messages).
    // This is passed to the model on every call so it has full conversational context.
    private readonly ChatHistory _chatHistory;

    /// <summary>All messages shown in the chat UI, in chronological order.</summary>
    public List<UiChatMessage> Messages { get; } = new();

    /// <summary>True while an AI call is in-flight. Used to disable the input and send button.</summary>
    public bool IsWaiting { get; private set; }

    /// <summary>
    /// True once IntroduceAsync() completes successfully. Prevents the user from sending
    /// messages before the AI is warmed up and the introductory message has been shown.
    /// </summary>
    public bool IsReady { get; private set; }

    /// <summary>Human-readable status text shown in the header status pill.</summary>
    public string Status { get; private set; } = "Connecting…";

    /// <summary>
    /// Machine-readable status kind used as a data-kind attribute on the status pill so CSS
    /// can colour the indicator dot. Values: "connecting", "thinking", "ready", "offline".
    /// </summary>
    public string StatusKind { get; private set; } = "connecting";

    /// <summary>
    /// Raised whenever any state property changes (IsWaiting, Status, Messages, etc.).
    /// Home.razor subscribes to this and calls InvokeAsync(StateHasChanged) to re-render.
    /// Because SK AI calls run on background threads, the handler must use InvokeAsync to
    /// marshal back onto the Blazor synchronisation context.
    /// </summary>
    public event Action? OnStateChanged;

    public ChatService(IChatCompletionService chatCompletionService, Kernel kernel)
    {
        _chatCompletionService = chatCompletionService;
        _kernel = kernel;
        _chatHistory = new ChatHistory();

        // Seed the conversation with the system prompt. This is the first message in every
        // chat history sent to the model and defines the assistant's persona and behaviour.
        _chatHistory.AddSystemMessage(Prompts.TempSystemPrompt);
    }

    /// <summary>
    /// Sends a hidden "Introduce yourself" prompt to the AI and adds the response to Messages
    /// as the first assistant turn. Called once on first render (fire-and-forget from
    /// OnAfterRenderAsync so it does not block the initial page paint).
    ///
    /// The user never sees the trigger prompt — only the assistant's reply is shown.
    /// After this completes, IsReady is set to true and the input becomes enabled.
    /// </summary>
    public async Task IntroduceAsync()
    {
        Status = "Meeting Semantigator…";
        StatusKind = "connecting";
        OnStateChanged?.Invoke();

        // addUserMessage: false → the "Introduce yourself" prompt is added to ChatHistory
        // but NOT shown as a client bubble in the UI.
        await SendCoreAsync("Introduce yourself", addUserMessage: false);

        IsReady = true;
        Status = "Ready";
        StatusKind = "ready";
        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Sends a user message to the AI. Adds the client bubble to Messages immediately
    /// (so the UI feels responsive), then awaits the AI response via SendCoreAsync.
    /// Guards against re-entrant calls while IsWaiting is true.
    /// </summary>
    public async Task SendMessageAsync(string text)
    {
        if (IsWaiting || !IsReady) return;

        // Show the user's own message right away — HTML-encode to prevent script injection
        // since assistant responses are later rendered as raw MarkupString.
        Messages.Add(new UiChatMessage
        {
            Role = "Client",
            Content = System.Web.HttpUtility.HtmlEncode(text),
            Timestamp = DateTime.Now
        });

        await SendCoreAsync(text, addUserMessage: true);
    }

    /// <summary>
    /// Core method that drives every AI call, shared by both IntroduceAsync and SendMessageAsync.
    ///
    /// Flow:
    ///   1. Set IsWaiting = true and notify the UI so the typing indicator appears.
    ///   2. Append the user message to ChatHistory (with /nothink suffix for user turns to
    ///      suppress chain-of-thought tokens that some models emit).
    ///   3. Call GetChatMessageContentAsync with AutoInvokeKernelFunctions so SK can
    ///      automatically execute any registered kernel plugins (e.g. RAG retrieval).
    ///   4. Clean the response and add it to both ChatHistory (for context) and Messages (for UI).
    ///   5. Always reset IsWaiting in the finally block so the UI never gets stuck.
    /// </summary>
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
                // Append /nothink to suppress internal reasoning tokens that some models
                // (e.g. DeepSeek-R1) emit before the final answer. Harmless for models
                // that don't support it.
                _chatHistory.AddUserMessage(text + " /nothink");
            else
                // Intro prompt — no suffix needed since it's not a real user turn.
                _chatHistory.AddUserMessage(text);

            var settings = new OpenAIPromptExecutionSettings
            {
                // Let SK automatically call any kernel functions (plugins) the model decides
                // to invoke. This is how RAG retrieval is triggered mid-conversation.
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            var result = await _chatCompletionService.GetChatMessageContentAsync(
                _chatHistory, settings, _kernel);

            sw.Stop();

            var content = CleanResponse(result.Content ?? "");

            // Add to ChatHistory so subsequent turns have this response as context.
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
            // Surface errors as a System bubble so the user knows something went wrong
            // without exposing a raw exception stack trace.
            Messages.Add(new UiChatMessage
            {
                Role = "System",
                Content = $"<em>An error occurred: {System.Web.HttpUtility.HtmlEncode(ex.Message)}</em>"
            });
        }
        finally
        {
            // Always unblock the UI regardless of success or failure.
            // IsReady may still be false here during IntroduceAsync; it is set to true
            // by the caller once this method returns successfully.
            IsWaiting = false;
            Status = IsReady ? "Ready" : "Initializing…";
            StatusKind = IsReady ? "ready" : "connecting";
            OnStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Strips artefacts that some models emit around HTML responses.
    ///
    /// Some models (particularly local/fine-tuned ones) wrap their HTML output in markdown
    /// code fences (```html ... ```) or include internal channel/constrain tokens that are
    /// part of their output format but should not be shown to the user. This method removes
    /// all of those so only the clean HTML reaches the chat bubble renderer.
    ///
    /// The <div> trimming handles models that prepend preamble text before the first tag —
    /// we discard everything before the opening &lt;div&gt; so only the HTML body is shown.
    /// </summary>
    private static string CleanResponse(string content)
    {
        // If the model prefixed the HTML with plain-text commentary, discard it.
        var divTagIndex = content.IndexOf("<div>");
        if (divTagIndex > -1)
            content = content[divTagIndex..];

        return content
            .Replace("```html", "")
            .Replace("```", "")
            // LM Studio / local model output format tokens
            .Replace("<|channel|>final <|constrain|>html<|message|>", "")
            .Replace("<|channel|>final <|constrain|>", "")
            .Replace("div<|message|>", "")
            .Replace("<|message|>", "")
            .Replace("commentary", "");
    }
}
