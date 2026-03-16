# MCP Client / “Agent” Project (SemanticSwamp.MCPBlazor)

This document describes the MCP-backed Blazor app: `SemanticSwamp.MCPBlazor`.

In this solution, “agent” refers to the client-side component that:

- drives the UI
- sends user messages / requests to the MCP server
- receives responses and renders them

The LLM and tool calling logic live on the MCP server (Semantic Kernel), but the client is still responsible for initiating and coordinating the protocol calls.

## 1. Goals of the MCP-backed client

Compared to `SemanticSwamp.Blazor`, the MCP variant:

- keeps the same UI concepts (chat, upload modal, documents modal)
- keeps the SQL Server access for UI dropdown data and downloads
- offloads AI-related work to `SemanticKernel.MCP` via MCP tools

This makes the UI process lighter and enables deployment patterns where:

- the UI runs in one host/process
- the AI stack runs separately (sidecar or separate node)

## 2. Key files

### 2.1 `Program.cs`

- registers Blazor Server services
- configures EF Core `SemanticSwampDBContext`
- registers MCP client (`IMcpClient`)
- registers:
  - `ChatService` (MCP chat)
  - `McpUploadManager` (upload + MCP processing)
  - `EntityService` (DB reads for UI)

### 2.2 `Services/McpProcessClient.cs`

This is the MCP transport implementation.

Current behavior:

- spawns the MCP server using `dotnet run --project ..\SemanticKernel.MCP\SemanticKernel.MCP.csproj`
- uses redirected stdin/stdout to send/receive JSON-RPC
- uses a mutex so only one request is in flight at a time

Important implementation details:

- stdout is treated as line-delimited JSON-RPC
- stderr is drained and retained (tail) for diagnostics
- non-JSON stdout lines are ignored

Configuration:

- `appsettings.json` contains `McpServer:Command`, `McpServer:Args`

### 2.3 `Services/ChatService.cs`

This ChatService is similar to the original Blazor ChatService but instead calls MCP:

- tool: `chat`
- tracks `sessionId` for multi-turn continuity

### 2.4 `Services/McpUploadManager.cs`

Implements `IFileManager` so the existing upload modal can keep calling `ProcessUpload()`.

Flow:

1. Save upload record in SQL (same pattern as original pipeline)
2. If PDF, call MCP `pdf_extract_text` to extract/repair text
3. Call MCP `summarize_document`
4. Call MCP `rag_upload_document` to embed+upsert into Qdrant

## 3. What remains local in the MCP client

The MCP client still talks directly to SQL Server for:

- collection/category/terms lists
- document list + summaries
- file download endpoint

Rationale:

- these operations are cheap and do not require the LLM
- they keep the UI responsive
- they avoid inflating the MCP tool surface with UI-only queries

## 4. What is delegated to the MCP server

The MCP server owns:

- the LLM endpoint configuration and model
- Semantic Kernel kernel + plugins
- chat history state (per session id)
- PDF repair logic (via existing PDFManager)
- RAG indexing/search dependencies (Qdrant, embedding model)

## 5. Running the MCP client

Recommended dev workflow:

- Start external dependencies: SQL Server, Qdrant, LM Studio
- Run `SemanticSwamp.MCPBlazor`
- The MCP client will start the MCP server process automatically

Alternative:

- run `SemanticKernel.MCP` separately
- adjust the client to connect via a different transport (named pipes / TCP / etc.)

## 6. Failure modes

Typical problems:

- MCP server fails to start (wrong working directory, missing secrets)
- MCP server exits early (unhandled exception on startup)
- long AI operations cause timeouts or memory pressure

The MCP process client captures stderr to make these issues diagnosable.
