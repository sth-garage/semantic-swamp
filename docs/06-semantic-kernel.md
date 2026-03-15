# Semantic Kernel

## Overview

[Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel) (SK) is the AI
orchestration layer of Semantic Swamp. It handles:

- Connecting to the LLM (LM Studio / OpenAI-compatible endpoint).
- Managing the `ChatHistory` for multi-turn conversations.
- Automatically invoking **plugins** (registered C# functions) when the model decides
  they are needed to answer a user's question.
- Generating text embeddings for RAG via a local in-process model.

All SK setup lives in `SemanticSwamp.SK\SKBuilder.cs`.

---

## SKBuilder — Kernel Construction

`SKBuilder.BuildSemanticKernel(ConfigurationValues)` is called **once at startup** in
`Program.cs`. It constructs the `Kernel` and extracts the services that are then registered
in the ASP.NET Core DI container as singletons.

### Key Configuration Choices

```csharp
// 1. OpenAI-compatible chat completion pointing at LM Studio
var skBuilder = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(
        modelId:   configValues.LMStudioSettings.LMStudio_Model,
        apiKey:    configValues.LMStudioSettings.LMStudio_ApiKey,
        endpoint:  new Uri(configValues.LMStudioSettings.LMStudio_ApiUrl),
        httpClient: new HttpClient { Timeout = TimeSpan.FromHours(2) }
        //                          ^^^^ 2 hours — local LLMs are slow on large docs
    )
    // 2. 384-dim local embeddings — no external API, runs in-process on server CPU
    .AddLocalTextEmbeddingGeneration();

// 3. Plugins — auto-invoked by the SK planner during chat turns
skBuilder.Plugins.AddFromType<DocumentUploadPlugin>();
skBuilder.Plugins.AddFromType<DocumentUploadSearchPlugin>();

// 4. Qdrant vector store
skBuilder.Services.AddSingleton<QdrantClient>(sp => new QdrantClient("localhost"));
skBuilder.Services.AddQdrantVectorStore();
```

### Why a 2-Hour Timeout?

Local LLM inference can take many minutes for large documents (e.g., summarising a full
copy of *The Odyssey*). The default `HttpClient` timeout of 100 seconds would cancel these
requests prematurely. The 2-hour timeout is a safe upper bound for development purposes.

### Extracted Services Registered in DI

After `BuildSemanticKernel` returns, `Program.cs` registers three singletons from the result:

| Service | Type | Notes |
|---|---|---|
| `IChatCompletionService` | Singleton | Used by `ChatService` and `UploadManager` |
| `ITextEmbeddingGenerationService` | Singleton | Used by `RAGManager` |
| `Kernel` | Singleton | Used by `ChatService` for plugin-aware chat |

---

## Plugins

SK plugins are plain C# classes with methods decorated with `[KernelFunction]`. When the AI
processes a chat turn, SK's **auto-function-calling** feature inspects all registered plugins
and invokes any functions the model determines are relevant to the user's message.

### `DocumentUploadPlugin`

**Purpose:** Gives the AI the ability to enumerate and read uploaded documents from the
SQL Server database.

| Kernel Function | Description | When Invoked |
|---|---|---|
| `list_document_upload_filenames` | Returns a `List<string>` of all uploaded filenames | AI asked "what files have been uploaded?" |
| `get_document_upload_by_filename` | Returns the full `DocumentUpload` entity (metadata + summary) for a specific file | AI needs metadata about a named file |
| `read_file_by_doc_upload_id` | Decodes `Base64Data` → UTF-8 string and returns the file's text content | AI asked to quote or reason over a specific file's full content |

### `DocumentUploadSearchPlugin`

**Purpose:** Gives the AI semantic search capability over all uploaded documents.

| Kernel Function | Description | When Invoked |
|---|---|---|
| `search_uploaded_documents` | Calls `RAGManager.Search(question)` → returns top-30 relevant chunks | AI determines the question requires searching document knowledge |

This is the primary RAG entry point during chat. The AI decides autonomously whether to
invoke this function based on the user's message and its description.

### `ExportPlugin`

**Purpose:** Lets the AI write text to a file on the server.

| Kernel Function | Description | When Invoked |
|---|---|---|
| `export_text` | Writes `textToExport` to `filePath` via `File.WriteAllText` | AI asked to save/export generated content |

> ⚠️ **Note:** `ExportText` uses `async void` (fire-and-forget). Exceptions from the write
> operation will not propagate to the caller. For production use the return type should be
> changed to `Task`.

---

## Chat Session Flow

The `ChatService` (Blazor project, scoped per circuit) manages the per-user conversation:

```
User types a message → ChatService.SendAsync(message)
    │
    ├─ Add user message to ChatHistory
    │
    ├─ Set IsWaiting = true, fire OnStateChanged (UI shows spinner)
    │
    ├─ Kernel.InvokePromptStreamingAsync(message, executionSettings)
    │       executionSettings.ToolCallBehavior = AutoInvokeKernelFunctions
    │       │
    │       │   SK calls the LLM, which may respond with tool_call requests
    │       │   SK intercepts those, invokes the matching plugin functions,
    │       │   feeds results back to the model, and streams the final response.
    │       │
    │       └─ Plugins that may fire: DocumentUploadSearchPlugin, DocumentUploadPlugin
    │
    ├─ Stream tokens into UiChatMessage.Content as they arrive
    │       → OnStateChanged fires each token → UI re-renders
    │
    └─ Add assistant message to ChatHistory, set IsWaiting = false
```

### System Prompt (Persona)

Every chat session begins with this system message (from `Prompts.TempSystemPrompt`):

> *"You are Semantigator, a highly intelligent and empathetic AI assistant designed to help
> individuals learn about AI and Semantic Kernel. All answers need to be rich HTML with the
> root node being a DIV. The answer must ONLY be an HTML div, no other text or comments."*

The HTML-div constraint is enforced because `Home.razor` renders AI responses via
`@((MarkupString)message.Content)` — the content is injected directly as HTML markup.
If the model returns plain text or markdown, the UI will display raw tags.

---

## Prompts Reference

All prompt strings are defined as constants in `SemanticSwamp.Shared.Prompts.Prompts`:

| Constant | Used By | Purpose |
|---|---|---|
| `TempSystemPrompt` | `ChatService` constructor | Establishes the "Semantigator" persona and HTML-only output constraint |
| `SummarizeText` | `UploadManager.GetTextSummary` | Prepended to document chunks when requesting a summary |
| `ResultAsRichHTMLDivRoot` | Available for use | Instructs the model to return a root `<div>` |
| `ResultCompleteResults` | Available for use | Instructs the model not to truncate its answer |
