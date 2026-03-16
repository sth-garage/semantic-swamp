using System.Diagnostics;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SemanticSwamp.MCPBlazor.Services;

public sealed class McpProcessClient : IMcpClient
{
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly string _workingDirectory;
    private readonly ConcurrentQueue<string> _stderrTail = new();
    private const int MaxStderrTailLines = 200;

    private Process? _process;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;

    private long _nextId = 1;

    public McpProcessClient(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        Configuration = configuration;

        _workingDirectory =
            Configuration["McpServer:WorkingDirectory"]
            ?? hostEnvironment.ContentRootPath
            ?? Directory.GetCurrentDirectory();
    }

    private IConfiguration Configuration { get; }

    public async Task<T> CallToolAsync<T>(string toolName, object? arguments = null, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            EnsureProcessStarted();

            var id = Interlocked.Increment(ref _nextId);

            var request = new
            {
                jsonrpc = "2.0",
                id,
                method = "tools/call",
                @params = new
                {
                    name = toolName,
                    arguments = arguments ?? new { }
                }
            };

            var json = JsonSerializer.Serialize(request, _jsonOptions);
            await _stdin!.WriteLineAsync(json.AsMemory(), cancellationToken);
            await _stdin.FlushAsync(cancellationToken);

            // Server is line-delimited: one JSON-RPC response per line.
            string? line;
            while ((line = await _stdout!.ReadLineAsync(cancellationToken)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(line);
                }
                catch (JsonException)
                {
                    // If the MCP server (or `dotnet run`) emits non-JSON lines on stdout,
                    // ignore them and keep reading for the actual JSON-RPC response.
                    continue;
                }

                using (doc)
                {
                    if (!doc.RootElement.TryGetProperty("id", out var respId)) continue;
                    if (respId.ValueKind != JsonValueKind.Number) continue;
                    if (respId.GetInt64() != id) continue;

                    if (doc.RootElement.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
                    {
                        var msg = err.TryGetProperty("message", out var m) ? m.GetString() : "Unknown MCP error";
                        throw new InvalidOperationException(msg);
                    }

                    var result = doc.RootElement.GetProperty("result");
                    var content = result.GetProperty("content");

                    if (content.ValueKind != JsonValueKind.Array || content.GetArrayLength() == 0)
                    {
                        return default!;
                    }

                    var text = content[0].GetProperty("text").GetString() ?? "";

                    // The MCP server wraps tool results as JSON-encoded text.
                    // Deserialize that JSON into the requested type.
                    return JsonSerializer.Deserialize<T>(text, _jsonOptions)!;
                }
            }

            if (_process is { HasExited: true })
            {
                throw new InvalidOperationException($"MCP server exited (code {_process.ExitCode}). Stderr tail:\n{GetStderrTail()}".Trim());
            }

            throw new InvalidOperationException($"MCP server closed stdout. Stderr tail:\n{GetStderrTail()}".Trim());
        }
        finally
        {
            _mutex.Release();
        }
    }

    private void EnsureProcessStarted()
    {
        if (_process is { HasExited: false } && _stdin != null && _stdout != null) return;

        DisposeProcess();

        // Defaults to: dotnet run --project ..\SemanticKernel.MCP\SemanticKernel.MCP.csproj
        var command = Configuration["McpServer:Command"];
        var args = Configuration["McpServer:Args"];

        if (string.IsNullOrWhiteSpace(command))
        {
            command = "dotnet";
        }

        if (string.IsNullOrWhiteSpace(args))
        {
            args = "run --project ..\\SemanticKernel.MCP\\SemanticKernel.MCP.csproj";
        }

        var psi = new ProcessStartInfo(command, args)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            // Use the app content root by default so relative paths like ..\SemanticKernel.MCP\... work.
            WorkingDirectory = _workingDirectory
        };

        _process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start MCP server process.");
        _stdin = _process.StandardInput;
        _stdout = _process.StandardOutput;

        // Fire-and-forget stderr drain so buffers don't fill up.
        _ = Task.Run(async () =>
        {
            try
            {
                while (_process is { HasExited: false })
                {
                    var line = await _process.StandardError.ReadLineAsync();
                    if (line is null) break;
                    AppendStderr(line);
                }
            }
            catch
            {
                // ignore
            }
        });
    }

    private void AppendStderr(string line)
    {
        _stderrTail.Enqueue(line);
        while (_stderrTail.Count > MaxStderrTailLines && _stderrTail.TryDequeue(out _))
        {
        }
    }

    private string GetStderrTail()
    {
        if (_stderrTail.IsEmpty) return "<empty>";
        return string.Join(Environment.NewLine, _stderrTail);
    }

    private void DisposeProcess()
    {
        try { _stdin?.Dispose(); } catch { }
        try { _stdout?.Dispose(); } catch { }

        if (_process != null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch { }
            finally
            {
                try { _process.Dispose(); } catch { }
            }
        }

        _process = null;
        _stdin = null;
        _stdout = null;
    }

    public ValueTask DisposeAsync()
    {
        DisposeProcess();
        _mutex.Dispose();
        return ValueTask.CompletedTask;
    }
}
