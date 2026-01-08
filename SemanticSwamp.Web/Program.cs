using Microsoft.EntityFrameworkCore;
using Microsoft.KernelMemory.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using SemanticSwamp.AppLogic;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.Shared.Interfaces;
using SemanticSwamp.Shared.Models;
using SemanticSwamp.Shared.Utility;
using SemanticSwamp.SK;
using SemanticSwamp.SK.RAG;

#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001


var webBuilder = WebApplication.CreateBuilder(args);

webBuilder.Services.AddControllers();

var configBuilder = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var configValues = UserSecretManager.GetSecrets(configBuilder);


SKBuilder skBuilder = new SKBuilder();
var semanticKernelBuildResult = await skBuilder.BuildSemanticKernel(configValues);

webBuilder.Services.AddDbContext<SemanticSwampDBContext>(options =>
{
    options.UseSqlServer(configValues.ConnectionStrings.ConnectionString_SemanticSwamp,
        sqlServerOptions => sqlServerOptions.CommandTimeout(600));
});

webBuilder.Services.AddSingleton<IChatCompletionService>(semanticKernelBuildResult.AIServices.ChatCompletionService);
webBuilder.Services.AddSingleton<Kernel>(semanticKernelBuildResult.AIServices.Kernel);
webBuilder.Services.AddSingleton<ConfigurationValues>(configValues);
webBuilder.Services.AddSingleton<ITextEmbeddingGenerationService>(semanticKernelBuildResult.AIServices.TextEmbeddingGenerationService); 
webBuilder.Services.AddScoped(typeof(ITextManager), typeof(TextManager));
webBuilder.Services.AddScoped(typeof(IRAGManager), typeof(RAGManager));
webBuilder.Services.AddScoped(typeof(IFileManager), typeof(UploadManager));


var app = webBuilder.Build();


// <snippet_UseWebSockets>
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(20)
};

app.UseWebSockets(webSocketOptions);
// </snippet_UseWebSockets>

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
