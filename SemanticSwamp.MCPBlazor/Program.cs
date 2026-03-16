using Microsoft.EntityFrameworkCore;
using SemanticSwamp.MCPBlazor.Components;
using SemanticSwamp.MCPBlazor.Services;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.Shared.Interfaces;
using SemanticSwamp.Shared.Models;
using SemanticSwamp.Shared.Utility;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(o => o.MaximumReceiveMessageSize = 10 * 1024 * 1024);

var configBuilder = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var configValues = UserSecretManager.GetSecrets(configBuilder);

builder.Services.AddDbContext<SemanticSwampDBContext>(options =>
{
    options.UseSqlServer(configValues.ConnectionStrings.ConnectionString_SemanticSwamp,
        sqlServerOptions => sqlServerOptions.CommandTimeout(6000));
});

builder.Services.AddSingleton(configValues);

// MCP client + app services
// Updated McpProcessClient constructor signature
builder.Services.AddSingleton<IMcpClient, McpProcessClient>();

builder.Services.AddSingleton<ITextManager, TextManager>();
builder.Services.AddScoped<IFileManager, McpUploadManager>();
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
