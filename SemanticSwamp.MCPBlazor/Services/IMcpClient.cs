namespace SemanticSwamp.MCPBlazor.Services;

public interface IMcpClient : IAsyncDisposable
{
    Task<T> CallToolAsync<T>(string toolName, object? arguments = null, CancellationToken cancellationToken = default);
}
