using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using SemanticSwamp.AppLogic;
using SemanticSwamp.Blazor.Services;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.Shared.Interfaces;
using SemanticSwamp.Shared.Models;
using SemanticSwamp.Shared.Utility;
using SemanticSwamp.SK;
using SemanticSwamp.SK.RAG;

#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(o => o.MaximumReceiveMessageSize = 10 * 1024 * 1024);

var configBuilder = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var configValues = UserSecretManager.GetSecrets(configBuilder);

SKBuilder skBuilder = new SKBuilder();
var semanticKernelBuildResult = await skBuilder.BuildSemanticKernel(configValues);

builder.Services.AddDbContext<SemanticSwampDBContext>(options =>
{
    options.UseSqlServer(configValues.ConnectionStrings.ConnectionString_SemanticSwamp,
        sqlServerOptions => sqlServerOptions.CommandTimeout(6000));
});

builder.Services.AddSingleton<QdrantClient>(sp => new QdrantClient("localhost"));
builder.Services.AddSingleton<IChatCompletionService>(semanticKernelBuildResult.AIServices.ChatCompletionService);
builder.Services.AddSingleton<Kernel>(semanticKernelBuildResult.AIServices.Kernel);
builder.Services.AddSingleton<ConfigurationValues>(configValues);
builder.Services.AddSingleton<ITextEmbeddingGenerationService>(semanticKernelBuildResult.AIServices.TextEmbeddingGenerationService);
builder.Services.AddScoped<ITextManager, TextManager>();
builder.Services.AddScoped<IRAGManager, RAGManager>();
builder.Services.AddScoped<IFileManager, UploadManager>();
builder.Services.AddScoped<IPDFManager, PDFManager>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<EntityService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/api/download/{id:int}", async (int id, SemanticSwampDBContext db) =>
{
    var doc = await db.DocumentUploads.FindAsync(id);
    if (doc?.Base64Data == null) return Results.NotFound();
    var bytes = Convert.FromBase64String(doc.Base64Data);
    return Results.File(bytes, "application/octet-stream", doc.FileName);
});

app.MapRazorComponents<SemanticSwamp.Blazor.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
