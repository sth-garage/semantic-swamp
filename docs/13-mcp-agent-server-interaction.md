# In-depth: MCP Client (“Agent”) ↔ MCP Server Interaction

This is a detailed guide to how the MCP client (`SemanticSwamp.MCPBlazor`) communicates with the MCP server (`SemanticKernel.MCP`) in this repository.

## 1. Roles and responsibilities

### MCP client responsibilities

- start or connect to an MCP server
- send JSON-RPC requests
- correlate responses by `id`
- map tool results into application behavior

### MCP server responsibilities

- parse JSON-RPC requests
- advertise available tools (`tools/list`)
- execute requested tools (`tools/call`)
- return tool results in MCP `content[]` format

## 2. Transport used here: child process + stdio

The MCP client spawns:

- `dotnet run --project ..\SemanticKernel.MCP\SemanticKernel.MCP.csproj`

Then it uses:

- redirected stdin to write requests
- redirected stdout to read responses
- redirected stderr for diagnostics

Important constraints:

- stdout must contain only protocol lines (or the client must ignore non-JSON output)
- one response is expected per request (line-delimited JSON)

## 3. Request/response flow

### 3.1 tools/call request shape

The client sends a JSON object like:

- `jsonrpc`: `"2.0"`
- `id`: number
- `method`: `"tools/call"`
- `params.name`: tool name
- `params.arguments`: object

The client increments ids for each call.

### 3.2 Correlation

The client reads stdout line-by-line until it finds a JSON-RPC response with matching `id`.

If it sees:

- blank lines: ignored
- non-JSON lines: ignored
- responses for other ids: ignored

### 3.3 Tool result decoding

The MCP server wraps the tool result into:

- `result.content[0].type = "text"`
- `result.content[0].text = <JSON string>`

The client then deserializes `text` into the requested type `T`.

## 4. Chat tool interaction

### 4.1 First message (introduce)

Sequence:

1. UI triggers `ChatService.IntroduceAsync()`
2. `ChatService` calls `IMcpClient.CallToolAsync("chat", { message: "Introduce yourself", sessionId: null })`
3. MCP server creates a new session id and returns it
4. Client stores `sessionId` for subsequent calls

### 4.2 Follow-up messages

For every user message:

- client sends tool call `chat` with the stored `sessionId`
- server retrieves the matching `ChatHistory` and appends new turns

This ensures multi-turn continuity.

## 5. Upload pipeline interaction (MCP-backed)

The MCP-backed upload pipeline is split between:

- SQL persistence (client)
- AI processing + embedding + indexing (server)

Typical PDF flow:

1. Client saves `DocumentUpload` row (Base64 PDF) to SQL
2. Client calls MCP `pdf_extract_text` with `documentUploadId`
3. Server reads Base64 from SQL, extracts page text via PdfPig, uses AI to fix ordering
4. Client calls MCP `summarize_document` with `overrideText`
5. Client calls MCP `rag_upload_document` with `overrideText`
6. Server chunks/embeds/upserts to Qdrant and marks the document processed

For non-PDF flow:

- steps 2–3 are skipped; summarization and RAG upload decode Base64 directly.

## 6. Error handling and diagnostics

### 6.1 Server exits

If the MCP server process exits, the client will eventually hit EOF on stdout.

The client captures stderr tail and surfaces it in the exception text so the UI can display the real cause.

### 6.2 Tool errors

If the server returns JSON-RPC `error`, the client throws an exception using the error message.

### 6.3 Non-protocol stdout

If `dotnet run` prints build output to stdout, it can corrupt protocol parsing.

The client mitigates this by ignoring lines that are not valid JSON.

For production-grade hosting:

- ensure the MCP server writes protocol output to stdout only
- direct all logs to stderr or to a file/structured logging sink

## 7. Compatibility contract

The client and server must agree on:

- tool names
- argument property names
- result JSON formats

If you change a tool schema on the server, update the corresponding client DTO shape.

## 8. Suggested next improvements

For long-term robustness:

- add a `ping` tool for health checks
- implement a reconnect strategy in the client
- move from `dotnet run` to running a published MCP server executable
- add a stable transport for production (pipes / sockets / TLS)
