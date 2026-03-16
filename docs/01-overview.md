# Semantic Swamp — Solution Overview

## What Is This?

**Semantic Swamp** is a .NET 10 Blazor Server application that lets users upload documents
(`.txt` or `.pdf`), have them summarised by a local AI model, and then chat with an AI
assistant that can intelligently search those documents to answer questions.

The name "Semantic Swamp" reflects the project's use of **Semantic Kernel** — Microsoft's
open-source SDK for integrating LLMs into .NET applications — combined with a playful
swamp theme throughout the UI.

---

## Key Capabilities

| Capability | How It Works |
|---|---|
| **Upload documents** | `.txt` and `.pdf` files are uploaded via a Blazor modal, stored as Base64 in SQL Server, and tagged with a Collection, Category, and keyword Terms. |
| **AI Summarisation** | Immediately after upload, the AI generates a plain-text summary of the document content. |
| **RAG Indexing** | Document text is chunked and vectorised using a local 384-dim embedding model, then stored in Qdrant for semantic search. |
| **AI Chat** | A Blazor chat UI connects to a local LLM (via LM Studio) powered by Semantic Kernel. The AI has plugins it can auto-invoke to list, read, and search uploaded documents. |
| **Document Browser** | A modal lists all uploaded documents with their summaries, allowing download of the original file. |

---

## Technology Stack

| Layer | Technology |
|---|---|
| UI | Blazor Server (.NET 10, `InteractiveServer` render mode) |
| AI Orchestration | [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel) |
| LLM Backend | [LM Studio](https://lmstudio.ai/) (OpenAI-compatible local endpoint) |
| Text Embeddings | SmartComponents `AddLocalTextEmbeddingGeneration()` — 384-dim in-process model |
| Vector Store | [Qdrant](https://qdrant.tech/) running on `localhost` |
| Relational Database | SQL Server (EF Core, code-first scaffolded) |
| PDF Extraction | [PdfPig](https://github.com/UglyToad/PdfPig) + AI column-reordering |
| CSS/JS | Bootstrap 5.3.3, Font Awesome 6.4, custom `swamp.css` / `swamp.js` |

---

## Project Structure

```
SemanticSwamp.sln
│
├── SemanticSwamp.Blazor      ← Startup project. Blazor Server UI, Program.cs, services.
├── SemanticSwamp.AppLogic    ← Upload pipeline orchestration (UploadManager, PDFManager).
├── SemanticSwamp.SK          ← Semantic Kernel setup, RAG manager, SK plugins.
├── SemanticSwamp.DAL         ← EF Core DbContext + entity models.
├── SemanticSwamp.Shared      ← DTOs, interfaces, utilities, prompts, enums. No UI/DB dependencies.
├── SemanticSwamp.Web         ← Legacy POC Web project (superseded by Blazor project).
│
├── SampleData/               ← Pre-bundled test files for the local-file smoke-test.
└── docs/                     ← ← You are here. Developer documentation.
```

Each project is described in detail in the remaining docs files:

- [`02-architecture.md`](02-architecture.md) — Layered architecture and dependency flow
- [`03-data-model.md`](03-data-model.md) — SQL Server schema and EF Core entities
- [`04-upload-pipeline.md`](04-upload-pipeline.md) — Document upload flow, step by step
- [`05-rag-pipeline.md`](05-rag-pipeline.md) — RAG indexing and search pipeline
- [`06-semantic-kernel.md`](06-semantic-kernel.md) — Semantic Kernel configuration and plugins
- [`07-blazor-ui.md`](07-blazor-ui.md) — Blazor UI structure, services, and state management
- [`08-setup-and-running.md`](08-setup-and-running.md) — Local development setup instructions

Additional reference documents:

- [`09-blazor-primer.md`](09-blazor-primer.md) — General Blazor Server primer (circuits, rendering, DI, JS interop)
- [`10-mcp-server.md`](10-mcp-server.md) — MCP server (`SemanticKernel.MCP`) design and tools
- [`11-mcp-agent-client.md`](11-mcp-agent-client.md) — MCP-backed Blazor client (`SemanticSwamp.MCPBlazor`)
- [`12-deployment-mcp.md`](12-deployment-mcp.md) — Deployment considerations for MCP server/client
- [`13-mcp-agent-server-interaction.md`](13-mcp-agent-server-interaction.md) — In-depth MCP client ↔ server interaction guide
