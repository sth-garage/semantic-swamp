# 🌿 Semantic Swamp

> **A hands-on .NET 10 learning project demonstrating Semantic Kernel, RAG, local LLMs, and Blazor Server — built and documented in public.**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Semantic Kernel](https://img.shields.io/badge/Semantic%20Kernel-latest-0078D4?logo=microsoft)](https://github.com/microsoft/semantic-kernel)
[![Qdrant](https://img.shields.io/badge/Qdrant-Vector%20DB-DC244C)](https://qdrant.tech/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## 👤 Author

**Scott Holt**
- 📝 Blog: [medium.com/@letslearnsk](https://medium.com/@letslearnsk) — follow along as the development of this project is chronicled post-by-post
- 💼 LinkedIn: [linkedin.com/in/scottholt1001](https://www.linkedin.com/in/scottholt1001/) — connect for updates, discussion, and .NET/AI content

---

## 🧠 What Is Semantic Swamp?

**Semantic Swamp** is a fully working .NET 10 reference application built to explore and demonstrate how to integrate **Microsoft Semantic Kernel**, **Retrieval-Augmented Generation (RAG)**, and a **local LLM** into a real Blazor Server application — without spending a cent on cloud AI APIs.

The project was built as a learning exercise and written about publicly on [Medium](https://medium.com/@letslearnsk). Every design decision, bug, and breakthrough is documented there so other developers can follow the same journey.

The name comes from the combination of **Semantic** Kernel and the playful swamp theme woven through the UI — because wrangling AI context windows sometimes feels like dragging things through a swamp.

---

## ✨ Key Features

| Feature | Description |
|---|---|
| 📄 **Document Upload** | Upload `.txt` or `.pdf` files via a polished Blazor modal. Files are tagged with a Collection, Category, and keyword Terms for organisation. |
| 🤖 **AI Summarisation** | Immediately after upload, a local LLM generates a plain-text summary of the document. Works with large files by chunking content across multiple chat messages. |
| 🔍 **RAG Indexing** | Document text is semantically chunked and vectorised using a local 384-dim embedding model, then stored in Qdrant for fast similarity search. |
| 💬 **AI Chat (Semantigator)** | Chat with an AI assistant persona called *Semantigator* that can autonomously list, read, and search your uploaded documents to answer questions. |
| 🗂️ **Document Browser** | Browse all uploaded documents, read AI-generated summaries, and download original files — all within the single-page Blazor UI. |
| 🧪 **Local File Smoke Test** | A developer-only test mode lets you run the full summarisation pipeline against pre-bundled sample files without uploading anything. |

---

## 🏗️ Architecture at a Glance

The solution is split into **six projects** in a strict dependency hierarchy — inner layers never depend on outer layers:

```
SemanticSwamp.Blazor        ← Startup project, UI, DI composition root
    ├── SemanticSwamp.AppLogic   ← Upload pipeline, PDF extraction
    ├── SemanticSwamp.SK         ← Semantic Kernel, RAG, plugins
    ├── SemanticSwamp.DAL        ← EF Core DbContext + SQL Server entities
    └── SemanticSwamp.Shared     ← Interfaces, DTOs, models, utilities (no dependencies)
```

All cross-project contracts are defined as **interfaces in `Shared`** and implemented
in the appropriate outer project — keeping the architecture clean, testable, and swappable.

See [`docs/02-architecture.md`](docs/02-architecture.md) for the full layered diagram and design decisions.

---

## 🛠️ Technology Stack

| Layer | Technology | Notes |
|---|---|---|
| **UI** | [Blazor Server](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) (.NET 10) | `InteractiveServer` render mode; scoped services per SignalR circuit |
| **AI Orchestration** | [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel) | Chat history, auto-function-calling, plugin system |
| **LLM Backend** | [LM Studio](https://lmstudio.ai/) | OpenAI-compatible local endpoint; swap to real OpenAI by changing config only |
| **Text Embeddings** | `SmartComponents.LocalEmbeddings` | 384-dim in-process model; no external API calls or cost |
| **Vector Store** | [Qdrant](https://qdrant.tech/) | Runs on `localhost`; single collection with metadata filtering |
| **Database** | SQL Server + [EF Core](https://learn.microsoft.com/en-us/ef/core/) | Scaffolded models; scoped DbContext per Blazor circuit |
| **PDF Extraction** | [PdfPig](https://github.com/UglyToad/PdfPig) + AI | Extracts text per page, then uses AI to correct multi-column ordering |
| **CSS / JS** | Bootstrap 5.3.3, Font Awesome 6.4 | Custom `swamp.css` and `swamp.js` for theme and JS interop |

---

## 📁 Project Structure

```
SemanticSwamp.sln
│
├── SemanticSwamp.Blazor/          ← Startup project
│   ├── Program.cs                 ← DI registrations, middleware, minimal API endpoints
│   ├── Components/
│   │   ├── Pages/Home.razor       ← The single page: chat UI + navbar
│   │   └── Modals/
│   │       ├── UploadModal.razor  ← File upload form
│   │       └── DocumentsModal.razor ← Document browser + download
│   ├── Services/
│   │   ├── ChatService.cs         ← Owns ChatHistory + message stream per circuit
│   │   └── EntityService.cs       ← Read-only DB facade for UI dropdowns
│   └── wwwroot/                   ← CSS, JS, images
│
├── SemanticSwamp.AppLogic/
│   ├── UploadManager.cs           ← Orchestrates the full upload pipeline
│   └── PDFManager.cs              ← PdfPig extraction + AI column correction
│
├── SemanticSwamp.SK/
│   ├── SKBuilder.cs               ← Builds and configures the Semantic Kernel
│   ├── RAG/
│   │   └── RAGManager.cs          ← Qdrant upsert + semantic search
│   └── Plugins/
│       ├── DocumentUploadPlugin.cs       ← AI can list and read documents
│       ├── DocumentUploadSearchPlugin.cs ← AI can search documents by semantic similarity
│       └── ExportPlugin.cs               ← AI can write output to a file
│
├── SemanticSwamp.DAL/
│   ├── Context/SemanticSwampDBContext.cs
│   └── EFModels/                  ← Collection, Category, Term, DocumentUpload, etc.
│
├── SemanticSwamp.Shared/
│   ├── Interfaces/                ← IFileManager, IRAGManager, IPDFManager, ITextManager
│   ├── Models/                    ← ConfigurationValues, RAG entry model, POCOs
│   ├── DTOs/                      ← FileUploadDTO, SimpleDocumentUpload
│   ├── Prompts/Prompts.cs         ← All AI prompt constants
│   └── Utility/                   ← TextManager (Base64, chunking), UserSecretManager
│
├── SampleData/                    ← Pre-bundled test files (Odyssey, movies list, etc.)
└── docs/                          ← Full developer documentation (see below)
```

---

## 🔄 How It Works — The Upload Pipeline

When you upload a document, the following happens automatically:

```
1. Browser picks file  →  IBrowserFile (Blazor)
2. Wrapped as IFormFile adapter  →  FileUploadDTO
3. UploadManager.ProcessUpload()
   ├─ Terms resolved / created in SQL Server
   ├─ Collection & Category resolved / created
   ├─ DocumentUpload row inserted
   ├─ If PDF: PdfPig extracts text → AI corrects column order
   ├─ AI generates a plain-text summary (chunked for large docs)
   └─ RAGManager.UploadToRAG()
       ├─ TextChunker splits text (128-token lines → 1024-token paragraphs)
       ├─ Local embedding model → 384-dim vectors per chunk
       └─ Upserted into Qdrant with metadata (CollectionId, CategoryId, TermIds)
```

See [`docs/04-upload-pipeline.md`](docs/04-upload-pipeline.md) and [`docs/05-rag-pipeline.md`](docs/05-rag-pipeline.md) for full detail.

---

## 💬 How It Works — AI Chat

Every chat message flows through Semantic Kernel with **auto-function-calling** enabled.
The AI (persona: *Semantigator*) can autonomously invoke three plugins:

| Plugin Function | What It Does |
|---|---|
| `list_document_upload_filenames` | Lists all uploaded document filenames |
| `get_document_upload_by_filename` | Fetches a document's metadata and AI summary |
| `read_file_by_doc_upload_id` | Reads the full decoded text content of a document |
| `search_uploaded_documents` | Runs a Qdrant vector similarity search scoped to the best-matching collection |

The AI decides on its own which functions to call. Before running a vector search, it also
asks itself which Collection best matches the question — narrowing the search to reduce noise.

See [`docs/06-semantic-kernel.md`](docs/06-semantic-kernel.md) for the full chat session flow.

---

## 🚀 Quick Start

> Full setup instructions with SQL schema DDL, Docker commands, and troubleshooting are in
> [`docs/08-setup-and-running.md`](docs/08-setup-and-running.md).

**Prerequisites:** .NET 10 SDK · SQL Server · Docker · [LM Studio](https://lmstudio.ai/)

```bash
# 1. Start Qdrant
docker run -d -p 6333:6333 -p 6334:6334 qdrant/qdrant

# 2. Start LM Studio → load a model → enable the local server (http://127.0.0.1:1234)

# 3. Configure user secrets (from the SemanticSwamp.Blazor directory)
dotnet user-secrets set "LMStudio_ApiKey"                "lm-studio"
dotnet user-secrets set "LMStudio_ApiUrl"                "http://127.0.0.1:1234/v1"
dotnet user-secrets set "LMStudio_Model"                 "your-model-id"
dotnet user-secrets set "ConnectionString_SemanticSwamp" "Data Source=...;TrustServerCertificate=True"

# 4. Run
dotnet run --project SemanticSwamp.Blazor
```

---

## 📚 Documentation

All developer documentation is in the [`docs/`](docs/) folder and registered as Solution
Items in Visual Studio's Solution Explorer:

| File | Contents |
|---|---|
| [`docs/01-overview.md`](docs/01-overview.md) | Capabilities, tech stack, project map |
| [`docs/02-architecture.md`](docs/02-architecture.md) | Layered architecture diagram, dependency rules, key design decisions |
| [`docs/03-data-model.md`](docs/03-data-model.md) | ER diagram, entity descriptions, EF Core context setup |
| [`docs/04-upload-pipeline.md`](docs/04-upload-pipeline.md) | Step-by-step upload flow, PDF extraction, IBrowserFile adapter |
| [`docs/05-rag-pipeline.md`](docs/05-rag-pipeline.md) | RAG indexing and search, Qdrant schema, TextChunker settings |
| [`docs/06-semantic-kernel.md`](docs/06-semantic-kernel.md) | SK kernel construction, plugins, chat session flow, prompts |
| [`docs/07-blazor-ui.md`](docs/07-blazor-ui.md) | Blazor project layout, pages, modals, services, JS interop |
| [`docs/08-setup-and-running.md`](docs/08-setup-and-running.md) | Full local dev setup: SQL DDL, Qdrant, LM Studio, user secrets, troubleshooting |

---

## 📖 Blog Series

This project is developed and documented in a public blog series on Medium.
Each post covers a specific aspect of the build — the reasoning, the mistakes, and the lessons learned.

👉 **Read the series:** [medium.com/@letslearnsk](https://medium.com/@letslearnsk)

Topics covered in the series include:
- Setting up Semantic Kernel with a local LLM in .NET
- Building a RAG pipeline with Qdrant and local embeddings
- Writing SK plugins and using auto-function-calling
- Blazor Server architecture patterns for AI applications
- PDF text extraction and AI-assisted column reordering
- Managing chat history and streaming responses in Blazor

---

## 🤝 Contributing & Contact

This is an open learning project. Questions, suggestions, and pull requests are welcome.

- **LinkedIn:** [linkedin.com/in/scottholt1001](https://www.linkedin.com/in/scottholt1001/) — best place for discussion and questions
- **Blog:** [medium.com/@letslearnsk](https://medium.com/@letslearnsk) — follow for updates as the project evolves
- **Issues:** Open a GitHub issue for bugs or feature ideas

---

## ⚖️ License

This project is licensed under the [MIT License](LICENSE).
