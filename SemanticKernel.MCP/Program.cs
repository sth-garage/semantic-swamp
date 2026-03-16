using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SemanticKernel.MCP.Chat;
using SemanticKernel.MCP.Mcp;
using SemanticKernel.MCP.Tools;
using SemanticSwamp.Shared.Utility;
using SemanticSwamp.SK;

var configRoot = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var configValues = UserSecretManager.GetSecrets(configRoot);

var skBuilder = new SKBuilder();
var kernelResult = await skBuilder.BuildSemanticKernel(configValues);

var scopeFactory = kernelResult.AIServices.Kernel.Services.GetRequiredService<IServiceScopeFactory>();

var tools = new SemanticSwampTools(scopeFactory);
var chat = new McpChatSessionManager(kernelResult.AIServices.Kernel, kernelResult.AIServices.ChatCompletionService);
var server = new McpServer(tools, chat);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await server.RunAsync(cts.Token);
