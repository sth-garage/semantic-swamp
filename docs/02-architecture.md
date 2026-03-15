# Architecture

## Layered Project Structure

The solution is split into six projects arranged in a strict dependency hierarchy.
Inner layers have **no knowledge** of outer layers — they only depend on `Shared` interfaces.

```
┌─────────────────────────────────────────────────────────────────┐
│                    SemanticSwamp.Blazor                         │
│   (UI, Program.cs, DI composition root, ChatService,           │
│    EntityService, Home.razor, UploadModal, DocumentsModal)      │
└────────────┬────────────────────────────────┬───────────────────┘
             │                                │
             ▼                                ▼
┌────────────────────────┐     ┌──────────────────────────────────┐
│  SemanticSwamp.AppLogic│     │       SemanticSwamp.SK           │
│  UploadManager         │     │  SKBuilder, RAGManager,          │
│  PDFManager            │     │  DocumentUploadPlugin,           │
└────────────┬───────────┘     │  DocumentUploadSearchPlugin,     │
             │                 │  ExportPlugin                    │
             │                 └────────────────┬─────────────────┘
             │                                  │
             └──────────┬───────────────────────┘
                        │
                        ▼
        ┌───────────────────────────────┐
        │       SemanticSwamp.DAL       │
        │  SemanticSwampDBContext       │
        │  EF Core entity models        │
        └───────────────┬───────────────┘
                        │
                        ▼
        ┌───────────────────────────────┐
        │     SemanticSwamp.Shared      │
        │  Interfaces (IFileManager,    │
        │  IRAGManager, IPDFManager,    │
        │  ITextManager)                │
        │  DTOs, Models, Prompts,       │
        │  Enums, Extensions,           │
        │  ConfigurationValues,         │
        │  TextManager, UserSecretMgr   │
        └───────────────────────────────┘
```

**Dependency rules:**

- `Shared` has **no dependencies** on any other project in the solution.
- `DAL` depends only on `Shared`.
- `AppLogic` depends on `DAL` and `Shared`.
- `SK` depends on `DAL` and `Shared`.
- `Blazor` depends on `AppLogic`, `SK`, `DAL`, and `Shared`. It is the **composition root** — the only place where concrete implementations are bound to interfaces in the DI container.

---

## Key Architectural Decisions

### 1. Blazor Server with Scoped Services per Circuit

The app uses Blazor Server (not WebAssembly). Each browser tab creates a SignalR circuit,
and scoped services are instantiated once per circuit. This means:

- `ChatService` holds the `ChatHistory` for one tab's conversation — no cross-user leakage.
- `SemanticSwampDBContext` is scoped so EF change tracking is isolated per circuit.
- Singleton services (`IChatCompletionService`, `Kernel`, `QdrantClient`) are shared across all circuits because they are stateless and expensive to construct.

### 2. Interfaces in Shared — Implementations Elsewhere

All cross-project contracts are defined as interfaces in `SemanticSwamp.Shared.Interfaces`.
This keeps the Shared project dependency-free and allows `AppLogic` and `SK` to be
swapped or mocked independently.

```
IFileManager  → UploadManager  (AppLogic)
IRAGManager   → RAGManager     (SK)
IPDFManager   → PDFManager     (AppLogic)
ITextManager  → TextManager    (Shared/Utility)
```

### 3. Base64 for File Storage

Files are stored as Base64 strings in the SQL Server `DocumentUploads.Base64Data` column.
This was chosen for simplicity (no separate file storage service needed) and to keep the
prototype self-contained. For production, this would be replaced with Azure Blob Storage
or a similar service.

### 4. Local LLM via LM Studio

The AI backend is [LM Studio](https://lmstudio.ai/), which exposes an OpenAI-compatible
REST API on `http://127.0.0.1:1234/v1`. Semantic Kernel uses `AddOpenAIChatCompletion`
with a custom endpoint URI, so switching to a real OpenAI API only requires changing the
`ConfigurationValues` — no code changes.

The HTTP client timeout is set to **2 hours** because local LLM inference over large
documents can be extremely slow.

### 5. Local Embeddings — No External Embedding API

Text embeddings are generated using `SmartComponents.LocalEmbeddings` (registered via
`AddLocalTextEmbeddingGeneration()`). This runs a 384-dimensional embedding model
in-process on the server CPU. No external API calls are made for embeddings.

This means:
- Embeddings are free (no API billing).
- Embedding speed depends on server CPU.
- The vector dimensionality is fixed at **384**.

---

## External Dependencies (Runtime)

| Dependency | Purpose | Where |
|---|---|---|
| **LM Studio** | Local LLM inference server | `http://127.0.0.1:1234` |
| **Qdrant** | Vector database | `localhost` (default gRPC port 6334) |
| **SQL Server** | Relational data | Connection string in user secrets |

All three must be running before the Blazor application will start correctly.
See [`08-setup-and-running.md`](08-setup-and-running.md) for setup instructions.
