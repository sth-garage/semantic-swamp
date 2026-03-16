using System.Text.Json;
using System.Text.Json.Serialization;
using SemanticKernel.MCP.Chat;
using SemanticKernel.MCP.Tools;

namespace SemanticKernel.MCP.Mcp;

public sealed class McpServer
{
    private readonly SemanticSwampTools _tools;
    private readonly McpChatSessionManager _chat;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpServer(SemanticSwampTools tools, McpChatSessionManager chat)
    {
        _tools = tools;
        _chat = chat;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await Console.In.ReadLineAsync(cancellationToken);
            if (line is null) return;
            if (string.IsNullOrWhiteSpace(line)) continue;

            JsonRpcRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<JsonRpcRequest>(line, _jsonOptions);
            }
            catch
            {
                continue;
            }

            if (request is null || string.IsNullOrWhiteSpace(request.Method)) continue;

            var response = await HandleRequestAsync(request, cancellationToken);

            // Notifications have no id.
            if (request.Id is null) continue;

            var json = JsonSerializer.Serialize(response, _jsonOptions);
            await Console.Out.WriteLineAsync(json);
            await Console.Out.FlushAsync(cancellationToken);
        }
    }

    private async Task<JsonRpcResponse> HandleRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken)
    {
        try
        {
            switch (request.Method)
            {
                case "initialize":
                    return Ok(request, new
                    {
                        protocolVersion = "2024-11-05",
                        serverInfo = new { name = "SemanticSwamp MCP", version = "0.1" },
                        capabilities = new { tools = new { } }
                    });

                case "tools/list":
                    return Ok(request, new { tools = GetToolDefinitions() });

                case "tools/call":
                    return await HandleToolsCallAsync(request, cancellationToken);

                case "shutdown":
                    return Ok(request, new { });

                default:
                    return Error(request, -32601, $"Method not found: {request.Method}");
            }
        }
        catch (Exception ex)
        {
            return Error(request, -32603, ex.Message);
        }
    }

    private async Task<JsonRpcResponse> HandleToolsCallAsync(JsonRpcRequest request, CancellationToken cancellationToken)
    {
        if (request.Params is null)
        {
            return Error(request, -32602, "Missing params");
        }

        var p = request.Params.Value;

        if (!p.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
        {
            return Error(request, -32602, "Missing params.name");
        }

        var name = nameElement.GetString() ?? "";
        var args = p.TryGetProperty("arguments", out var argumentsElement) ? argumentsElement : default;

        object toolResult;

        switch (name)
        {
            case "chat":
            {
                var message = args.ValueKind != JsonValueKind.Undefined && args.TryGetProperty("message", out var m)
                    ? m.GetString() ?? ""
                    : "";

                var sessionId = args.ValueKind != JsonValueKind.Undefined && args.TryGetProperty("sessionId", out var sid)
                    ? sid.GetString()
                    : null;

                var reset = args.ValueKind != JsonValueKind.Undefined && args.TryGetProperty("reset", out var r) && r.ValueKind == JsonValueKind.True;

                if (string.IsNullOrWhiteSpace(message))
                    return Error(request, -32602, "arguments.message is required");

                toolResult = await _chat.SendAsync(message, sessionId, reset, cancellationToken);
                break;
            }

            case "list_document_upload_filenames":
                toolResult = await _tools.ListDocumentUploadFilenamesAsync(cancellationToken);
                break;

            case "get_document_upload_by_filename":
            {
                var fileName = args.ValueKind != JsonValueKind.Undefined && args.TryGetProperty("fileName", out var fn)
                    ? fn.GetString() ?? ""
                    : "";

                if (string.IsNullOrWhiteSpace(fileName))
                    return Error(request, -32602, "arguments.fileName is required");

                toolResult = await _tools.GetDocumentUploadByFilenameAsync(fileName, cancellationToken);
                break;
            }

            case "read_file_by_doc_upload_id":
            {
                if (args.ValueKind == JsonValueKind.Undefined || !args.TryGetProperty("documentUploadId", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
                    return Error(request, -32602, "arguments.documentUploadId is required");

                toolResult = await _tools.ReadFileByDocUploadIdAsync(idEl.GetInt32(), cancellationToken);
                break;
            }

            case "search_uploaded_documents":
            {
                var question = args.ValueKind != JsonValueKind.Undefined && args.TryGetProperty("question", out var q)
                    ? q.GetString() ?? ""
                    : "";

                if (string.IsNullOrWhiteSpace(question))
                    return Error(request, -32602, "arguments.question is required");

                toolResult = await _tools.SearchUploadedDocumentsAsync(question, cancellationToken);
                break;
            }

            case "summarize_document":
            {
                if (args.ValueKind == JsonValueKind.Undefined || !args.TryGetProperty("documentUploadId", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
                    return Error(request, -32602, "arguments.documentUploadId is required");

                var overrideText = args.TryGetProperty("overrideText", out var ot) && ot.ValueKind == JsonValueKind.String
                    ? ot.GetString()
                    : null;

                toolResult = await _tools.SummarizeDocumentAsync(idEl.GetInt32(), overrideText, cancellationToken);
                break;
            }

            case "pdf_extract_text":
            {
                if (args.ValueKind == JsonValueKind.Undefined || !args.TryGetProperty("documentUploadId", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
                    return Error(request, -32602, "arguments.documentUploadId is required");

                toolResult = await _tools.ExtractPdfTextAsync(idEl.GetInt32(), cancellationToken);
                break;
            }

            case "rag_upload_document":
            {
                if (args.ValueKind == JsonValueKind.Undefined || !args.TryGetProperty("documentUploadId", out var idEl) || idEl.ValueKind != JsonValueKind.Number)
                    return Error(request, -32602, "arguments.documentUploadId is required");

                var overrideText = args.TryGetProperty("overrideText", out var ot) && ot.ValueKind == JsonValueKind.String
                    ? ot.GetString()
                    : null;

                toolResult = await _tools.UploadDocumentToRagAsync(idEl.GetInt32(), overrideText, cancellationToken);
                break;
            }

            default:
                return Error(request, -32601, $"Unknown tool: {name}");
        }

        // MCP tool responses return a content array. We encode the actual result as JSON text.
        var payloadJson = JsonSerializer.Serialize(toolResult, _jsonOptions);

        return Ok(request, new
        {
            content = new[]
            {
                new { type = "text", text = payloadJson }
            }
        });
    }

    private static JsonRpcResponse Ok(JsonRpcRequest request, object result) => new()
    {
        Id = request.Id,
        Result = result,
        Error = null
    };

    private static JsonRpcResponse Error(JsonRpcRequest request, int code, string message) => new()
    {
        Id = request.Id,
        Result = null,
        Error = new JsonRpcError { Code = code, Message = message }
    };

    private static List<McpToolDefinition> GetToolDefinitions() =>
    [
        new McpToolDefinition
        {
            Name = "chat",
            Description = "Chat with Semantigator using the configured Semantic Kernel (auto tool-calling enabled). Returns a sessionId to continue the conversation.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    message = new { type = "string", description = "The user message." },
                    sessionId = new { type = "string", description = "Optional conversation session id from a prior call." },
                    reset = new { type = "boolean", description = "If true, clears any existing history for this sessionId." }
                },
                required = new[] { "message" }
            }
        },
        new McpToolDefinition
        {
            Name = "list_document_upload_filenames",
            Description = "Returns a list of uploaded document filenames.",
            InputSchema = new { type = "object", properties = new { } }
        },
        new McpToolDefinition
        {
            Name = "get_document_upload_by_filename",
            Description = "Returns document metadata for a given filename.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    fileName = new { type = "string", description = "The exact filename to lookup." }
                },
                required = new[] { "fileName" }
            }
        },
        new McpToolDefinition
        {
            Name = "read_file_by_doc_upload_id",
            Description = "Returns the plain-text contents of an uploaded document by its id.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    documentUploadId = new { type = "integer", description = "The DocumentUpload id." }
                },
                required = new[] { "documentUploadId" }
            }
        },
        new McpToolDefinition
        {
            Name = "search_uploaded_documents",
            Description = "Semantic (vector) search over uploaded documents.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    question = new { type = "string", description = "The question or search query." }
                },
                required = new[] { "question" }
            }
        }
        ,
        new McpToolDefinition
        {
            Name = "summarize_document",
            Description = "Generates a plain-text summary for a stored DocumentUpload record.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    documentUploadId = new { type = "integer", description = "The DocumentUpload id." },
                    overrideText = new { type = "string", description = "Optional text to summarize instead of decoding Base64Data." }
                },
                required = new[] { "documentUploadId" }
            }
        },
        new McpToolDefinition
        {
            Name = "pdf_extract_text",
            Description = "Extracts and reconstructs the reading order of a stored PDF DocumentUpload.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    documentUploadId = new { type = "integer", description = "The DocumentUpload id." }
                },
                required = new[] { "documentUploadId" }
            }
        },
        new McpToolDefinition
        {
            Name = "rag_upload_document",
            Description = "Chunks, embeds, and upserts a stored DocumentUpload into the Qdrant vector store.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    documentUploadId = new { type = "integer", description = "The DocumentUpload id." },
                    overrideText = new { type = "string", description = "Optional text to index instead of decoding Base64Data." }
                },
                required = new[] { "documentUploadId" }
            }
        }
    ];
}
