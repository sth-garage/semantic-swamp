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

// Suppress experimental-API warnings from the Semantic Kernel SDK.
// SKEXP0010 covers OpenAI execution settings; SKEXP0001 covers embedding service interfaces.
// Both are stable enough for production use here despite their experimental designation.
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Blazor / SignalR setup
// ---------------------------------------------------------------------------

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    // Increase the SignalR hub message size limit to 10 MB so that file uploads
    // (passed as Base64 via IBrowserFile) do not hit the default 32 KB cap.
    .AddHubOptions(o => o.MaximumReceiveMessageSize = 10 * 1024 * 1024);

// ---------------------------------------------------------------------------
// Configuration — user secrets
// ---------------------------------------------------------------------------

// User secrets are stored outside the repository (in %APPDATA%\Microsoft\UserSecrets\)
// so API keys and connection strings are never committed to source control.
// UserSecretManager maps the raw IConfiguration keys into a typed ConfigurationValues object.
var configBuilder = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var configValues = UserSecretManager.GetSecrets(configBuilder);

// ---------------------------------------------------------------------------
// Semantic Kernel — build the AI kernel and extract its services
// ---------------------------------------------------------------------------

// SKBuilder reads the OpenAI / LM Studio settings from ConfigurationValues and constructs
// the SK Kernel with the appropriate chat completion and embedding connectors configured.
// This is done once at startup because the underlying HTTP clients are expensive to create.
SKBuilder skBuilder = new SKBuilder();
var semanticKernelBuildResult = await skBuilder.BuildSemanticKernel(configValues);

// ---------------------------------------------------------------------------
// Dependency injection registrations
// ---------------------------------------------------------------------------

// EF Core DbContext — scoped so each Blazor circuit gets its own tracked instance.
// CommandTimeout is raised to 6000 seconds to accommodate long-running embedding operations
// that may be triggered during document processing.
builder.Services.AddDbContext<SemanticSwampDBContext>(options =>
{
    options.UseSqlServer(configValues.ConnectionStrings.ConnectionString_SemanticSwamp,
        sqlServerOptions => sqlServerOptions.CommandTimeout(6000));
});

// Qdrant vector database client — singleton because it is thread-safe and holds a gRPC channel.
builder.Services.AddSingleton<QdrantClient>(sp => new QdrantClient("localhost"));

// Semantic Kernel services — registered as singletons because they are stateless and
// expensive to construct (they hold pre-built HTTP clients and embedding models).
builder.Services.AddSingleton<IChatCompletionService>(semanticKernelBuildResult.AIServices.ChatCompletionService);
builder.Services.AddSingleton<Kernel>(semanticKernelBuildResult.AIServices.Kernel);
builder.Services.AddSingleton<ConfigurationValues>(configValues);
builder.Services.AddSingleton<ITextEmbeddingGenerationService>(semanticKernelBuildResult.AIServices.TextEmbeddingGenerationService);

// Application-logic services — scoped so they share the same DbContext instance within
// a single Blazor circuit and are disposed cleanly when the circuit ends.
builder.Services.AddScoped<ITextManager, TextManager>();
builder.Services.AddScoped<IRAGManager, RAGManager>();
builder.Services.AddScoped<IFileManager, UploadManager>();   // handles .txt / .pdf ingestion + vectorisation
builder.Services.AddScoped<IPDFManager, PDFManager>();        // PDF text extraction
builder.Services.AddScoped<ChatService>();                    // owns the SK ChatHistory for one browser tab
builder.Services.AddScoped<EntityService>();                  // thin read facade over the DbContext

// ---------------------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------------------

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // HSTS tells browsers to only ever use HTTPS for this origin for the next year.
    app.UseHsts();
}

app.UseStaticFiles();   // serves wwwroot (swamp.css, swamp.js, Images/)
app.UseAntiforgery();   // required by Blazor's form handling to prevent CSRF

// ---------------------------------------------------------------------------
// Minimal API endpoints
// ---------------------------------------------------------------------------

// Download endpoint for document files stored as Base64 in the database.
// The DocumentsModal calls window.downloadFile('/api/download/{id}', fileName) via JS interop
// which triggers a browser download without leaving the SPA.
app.MapGet("/api/download/{id:int}", async (int id, SemanticSwampDBContext db) =>
{
    var doc = await db.DocumentUploads.FindAsync(id);
    if (doc?.Base64Data == null) return Results.NotFound();
    var bytes = Convert.FromBase64String(doc.Base64Data);
    return Results.File(bytes, "application/octet-stream", doc.FileName);
});

// Map Razor components and enable the interactive server render mode so components
// annotated with @rendermode InteractiveServer get a SignalR circuit.
app.MapRazorComponents<SemanticSwamp.Blazor.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
