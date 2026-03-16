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
    private readonly ConcurrentQueue<string> _stdoutTail = new();
    private const int MaxStderrTailLines = 200;
    private const int MaxStdoutTailLines = 200;

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
            Exception? lastException = null;

            // First try: start with --no-build to avoid build-time file locks when a second MCP instance is already running.
            // Fallback: retry with a build if the project isn't built yet.
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var startMode = attempt == 0 ? StartMode.NoBuild : StartMode.Build;
                EnsureProcessStarted(startMode);

                try
                {
                    return await CallToolOnceAsync<T>(toolName, arguments, cancellationToken);
                }
                catch (Exception ex) when (attempt == 0 && _lastStartUsedDefaultArgs && OutputIndicatesNoBuildFailure())
                {
                    lastException = ex;
                    DisposeProcess();
                    continue;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    throw;
                }
            }

            throw lastException ?? new InvalidOperationException("MCP call failed.");
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<T> CallToolOnceAsync<T>(string toolName, object? arguments, CancellationToken cancellationToken)
    {
        ThrowIfProcessExited();

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

        try
        {
            await _stdin!.WriteLineAsync(json.AsMemory(), cancellationToken);
            await _stdin.FlushAsync(cancellationToken);
        }
        catch (Exception)
        {
            ThrowIfProcessExited();
            throw;
        }

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
                // `dotnet run`/MSBuild output is not JSON; keep a tail so we can show it on failure.
                AppendStdout(line);
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

        ThrowIfProcessExited();

        throw new InvalidOperationException($"MCP server closed stdout. Stdout tail:\n{GetStdoutTail()}\n\nStderr tail:\n{GetStderrTail()}".Trim());
    }

    private enum StartMode
    {
        NoBuild,
        Build
    }

    private bool _lastStartUsedDefaultArgs;

    private void EnsureProcessStarted(StartMode startMode)
    {
        if (_process is { HasExited: false } && _stdin != null && _stdout != null) return;

        DisposeProcess();
        ResetOutputTails();

        // Defaults to: dotnet run --project ..\SemanticKernel.MCP\SemanticKernel.MCP.csproj
        var command = Configuration["McpServer:Command"];
        var args = Configuration["McpServer:Args"];

        if (string.IsNullOrWhiteSpace(command))
        {
            command = "dotnet";
        }

        if (string.IsNullOrWhiteSpace(args))
        {
            _lastStartUsedDefaultArgs = true;
            args = startMode == StartMode.NoBuild
                ? "run --no-build --project ..\\SemanticKernel.MCP\\SemanticKernel.MCP.csproj"
                : "run --project ..\\SemanticKernel.MCP\\SemanticKernel.MCP.csproj";
        }
        else
        {
            _lastStartUsedDefaultArgs = false;
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

    private void AppendStdout(string line)
    {
        _stdoutTail.Enqueue(line);
        while (_stdoutTail.Count > MaxStdoutTailLines && _stdoutTail.TryDequeue(out _))
        {
        }
    }

    private string GetStderrTail()
    {
        if (_stderrTail.IsEmpty) return "<empty>";
        return string.Join(Environment.NewLine, _stderrTail);
    }

    private string GetStdoutTail()
    {
        if (_stdoutTail.IsEmpty) return "<empty>";
        return string.Join(Environment.NewLine, _stdoutTail);
    }

    private void ResetOutputTails()
    {
        while (_stderrTail.TryDequeue(out _)) { }
        while (_stdoutTail.TryDequeue(out _)) { }
    }

    private void ThrowIfProcessExited()
    {
        if (_process is not { HasExited: true }) return;

        throw new InvalidOperationException(
            $"MCP server exited (code {_process.ExitCode}). Stdout tail:\n{GetStdoutTail()}\n\nStderr tail:\n{GetStderrTail()}".Trim());
    }

    private bool OutputIndicatesNoBuildFailure()
    {
        var combined = (GetStdoutTail() + "\n" + GetStderrTail()).ToLowerInvariant();

        return combined.Contains("unable to run your project")
            || combined.Contains("not ready to run")
            || combined.Contains("build the project")
            || combined.Contains("--no-build")
            || combined.Contains("restore") && combined.Contains("nuget");
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
