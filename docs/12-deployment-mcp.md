# Deployment Considerations (MCP server + client)

This document covers deployment considerations for running the MCP server (`SemanticKernel.MCP`) and an MCP client (`SemanticSwamp.MCPBlazor`).

## 1. Process topology options

### Option A: Sidecar process (same machine / same container)

- UI host and MCP server run on the same node
- The UI spawns the MCP server (current development approach)

Pros:

- simplest configuration
- no network hops
- secrets remain local to node

Cons:

- lifecycle coupling (UI restart kills MCP)
- scaling is tied (can’t scale MCP independently)
- stdout/stdin transport is fragile if the child process writes non-protocol output

### Option B: Separate service (recommended for production)

- MCP server runs as its own service/process
- UI connects to it over a stable transport

Pros:

- independent scaling
- independent deployment
- better observability and health checks

Cons:

- requires a transport other than stdin/stdout (e.g., named pipes, TCP, or an HTTP bridge)

### Option C: “AI node pool”

- multiple MCP servers run behind a scheduler or queue
- UI submits work items

Pros:

- isolates expensive inference workloads
- can rate limit and control concurrency

Cons:

- higher complexity

## 2. Transport considerations

Current implementation uses:

- child process + stdin/stdout line-delimited JSON-RPC

For production, consider:

- named pipes (Windows)
- Unix domain sockets (Linux)
- TCP with TLS
- an HTTP gateway that translates HTTP calls into MCP tool calls

Key requirements for a production transport:

- request correlation
- backpressure and bounded queues
- robust handling for logs vs protocol output

## 3. Secrets and configuration

In development, User Secrets are used.

In deployment, use environment-appropriate secret stores:

- environment variables
- Azure Key Vault
- Kubernetes secrets

Ensure both processes have access to:

- SQL connection string
- LLM endpoint settings (LM Studio / OpenAI / Azure OpenAI)
- Qdrant endpoint settings

If MCP runs separately, do not rely on “shared” UserSecretsId; deploy secrets explicitly to each host.

## 4. Resource sizing and limits

### 4.1 Memory

- embedding model loads into process memory
- LLM calls can produce large responses
- PDF extraction can buffer large strings

Plan for:

- high baseline memory usage for MCP server
- memory spikes during upload processing

### 4.2 Timeouts

- Local inference can be slow
- Long PDF repair / summarization requires high HTTP timeouts

Ensure:

- HTTP client timeouts are configured appropriately
- any reverse proxies (IIS, Nginx, Azure Front Door) are configured for long requests if using HTTP-based transports

### 4.3 Concurrency

- Local LLM servers often do not scale linearly with concurrency
- Qdrant can handle many queries, but embedding generation is CPU-bound

For production:

- add a concurrency gate (semaphore) per MCP server
- scale out by running multiple MCP servers rather than overloading one

## 5. Observability

Recommended:

- structured logging for tool calls
- capture tool latencies
- expose a health check endpoint (if using HTTP) or a dedicated `ping` tool

For process-based hosting:

- ensure stderr is captured and persisted
- ensure stdout is reserved for protocol data only

## 6. Deployment checklist

- Qdrant is reachable
- SQL Server is reachable
- LLM endpoint is reachable
- secrets are injected for both client and server
- client and server versions are compatible (tool names + schemas)
- MCP server has a bounded concurrency strategy
- logs are captured and searchable
