# MCP Server (SemanticKernel.MCP)

This document describes the **MCP server** project in this solution: `SemanticKernel.MCP`.

## 1. What MCP is (in this solution)

MCP (Model Context Protocol) is used here as a lightweight **tool server** accessible over a JSON-RPC interface.

In this repository, the MCP server:

- reads JSON-RPC requests from **stdin** (line-delimited)
- writes JSON-RPC responses to **stdout** (line-delimited)
- exposes a set of **tools** that wrap Semantic Swamp capabilities

The MCP server is designed to be started as a separate process and controlled by a client.

## 2. Transport and protocol details

### 2.1 Line-delimited JSON-RPC

`SemanticKernel.MCP` expects:

- one JSON object per line
- valid JSON-RPC 2.0 shape

Example request (tools/call):

- `method`: `tools/call`
- `params.name`: tool name
- `params.arguments`: tool arguments object

### 2.2 Response shape

Responses follow JSON-RPC 2.0.

Tool responses are encoded in MCP style as:

- `result.content`: an array
- each entry has `type` (text) and `text`

This implementation serializes the tool result as JSON and places it in `content[0].text`.

## 3. Project responsibilities

`SemanticKernel.MCP` is responsible for:

- loading configuration (User Secrets)
- building the Semantic Kernel stack via `SemanticSwamp.SK` (`SKBuilder`)
- hosting a JSON-RPC loop (`McpServer`) that dispatches tool calls

It is not responsible for:

- HTTP hosting
- Blazor UI

## 4. Key files

- `Program.cs`
  - loads secrets
  - builds Semantic Kernel and service provider
  - creates `McpServer` and runs it

- `Mcp/McpServer.cs`
  - JSON-RPC request parsing
  - method dispatch:
    - `initialize`
    - `tools/list`
    - `tools/call`
    - `shutdown`

- `Tools/SemanticSwampTools.cs`
  - concrete tool implementations
  - uses DI scope (`IServiceScopeFactory`) to resolve DB + RAG services safely per call

- `Chat/McpChatSessionManager.cs`
  - session-based chat history management
  - calls SK chat completion with tool-calling enabled

## 5. Tools exposed by the MCP server

### 5.1 Chat tool

- Tool: `chat`
- Purpose: chat with the configured Semantic Kernel assistant
- Supports session continuity via a `sessionId`

Notable behavior:

- Uses `ToolCallBehavior.AutoInvokeKernelFunctions`
- Preserves history per `sessionId` so the model retains context

### 5.2 Document tools

These tools wrap the document upload storage in SQL:

- `list_document_upload_filenames`
- `get_document_upload_by_filename`
- `read_file_by_doc_upload_id`

### 5.3 RAG tool

- `search_uploaded_documents`

This uses the existing `IRAGManager` implementation to search Qdrant.

### 5.4 Upload processing support tools (AI offload)

These tools exist primarily to support the MCP-backed Blazor client:

- `pdf_extract_text`
  - extracts PDF text and reconstructs reading order (PdfPig + AI)
- `summarize_document`
  - generates a plain-text summary
- `rag_upload_document`
  - chunks/embeds/upserts into Qdrant and marks the DB record as processed

## 6. Lifetime and scoping

The MCP server creates tools with an `IServiceScopeFactory` so each tool call can:

- create a fresh DI scope
- resolve `SemanticSwampDBContext` safely
- avoid leaking EF tracking state across tool calls

## 7. Running the MCP server

From the repo root:

- `dotnet run --project SemanticKernel.MCP`

The server will wait for JSON-RPC lines on stdin. When run by an MCP client (like `SemanticSwamp.MCPBlazor`), the client will manage stdin/stdout.

## 8. Troubleshooting

Common causes of MCP startup failures:

- invalid User Secrets values (missing connection string / model config)
- Qdrant not running
- LM Studio not running or wrong URL/model
- `dotnet run` executed with an unexpected working directory so relative paths break

When started by a client process, ensure you capture and inspect stderr from the MCP server process.
