
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.Shared.Interfaces;
using SemanticSwamp.Shared.Models;
using SemanticSwamp.Shared.Utility;
using SemanticSwamp.SK.Plugins;
using SemanticSwamp.SK.RAG;
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050


namespace SemanticSwamp.SK
{
    /// <summary>
    /// Constructs and wires up a fully configured Semantic Kernel instance along with all
    /// dependent services needed by the application (chat completion, text embeddings, Qdrant, plugins).
    /// <para>
    /// The kernel is built once at application startup in <c>Program.cs</c> and its extracted
    /// services (<see cref="IChatCompletionService"/>, <see cref="ITextEmbeddingGenerationService"/>,
    /// <see cref="Kernel"/>) are registered in the DI container for use across the application.
    /// </para>
    /// </summary>
    public class SKBuilder
    {
        /// <summary>
        /// Builds the Semantic Kernel and registers all required services.
        /// <para>
        /// Key decisions made here:
        /// <list type="bullet">
        ///   <item>
        ///     <b>OpenAI-compatible endpoint:</b> The app targets a local LM Studio server, which exposes
        ///     an OpenAI-compatible REST API. <c>AddOpenAIChatCompletion</c> is used with a custom
        ///     <c>endpoint</c> URI pointing to localhost.
        ///   </item>
        ///   <item>
        ///     <b>2-hour HTTP timeout:</b> Local LLM inference for large documents can be very slow.
        ///     The timeout is set to 2 hours to prevent premature cancellation during summarisation.
        ///   </item>
        ///   <item>
        ///     <b>Local text embeddings:</b> <c>AddLocalTextEmbeddingGeneration</c> uses SmartComponents
        ///     to run a 384-dimension embedding model in-process, avoiding any external embedding API calls.
        ///   </item>
        ///   <item>
        ///     <b>Plugins:</b> <c>DocumentUploadPlugin</c> lets the AI list and read uploaded documents;
        ///     <c>DocumentUploadSearchPlugin</c> lets the AI perform semantic RAG searches.
        ///     Both are auto-invoked by Semantic Kernel during chat turns.
        ///   </item>
        ///   <item>
        ///     <b>Qdrant:</b> A singleton <c>QdrantClient</c> pointing to localhost is registered
        ///     alongside the Qdrant vector store abstraction from Semantic Kernel.
        ///   </item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="configValues">Typed configuration (LM Studio URL, model ID, API key, connection strings) loaded from user secrets.</param>
        /// <returns>A <see cref="SemanticKernelBuilderResult"/> containing the built kernel and extracted AI services.</returns>
        public async Task<SemanticKernelBuilderResult> BuildSemanticKernel(ConfigurationValues configValues)
        {
            var modelId = configValues.LMStudioSettings.LMStudio_Model;
            var apiKey = configValues.LMStudioSettings.LMStudio_ApiKey;
            var apiUrl = configValues.LMStudioSettings.LMStudio_ApiUrl;

            HttpClient client = new HttpClient()
            {
                Timeout = new TimeSpan(2, 0, 0)
            };

            var skBuilder = Kernel.CreateBuilder().AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: apiKey,
                endpoint: new Uri(apiUrl),
                httpClient: client
            ).AddLocalTextEmbeddingGeneration();

            skBuilder.Services.AddDbContext<SemanticSwampDBContext>(options =>
            {
                options.UseSqlServer(configValues.ConnectionStrings.ConnectionString_SemanticSwamp,
                    sqlServerOptions => sqlServerOptions.CommandTimeout(600));
            });

            skBuilder.Services.AddSingleton<ConfigurationValues>(configValues);

            // Plugins
            skBuilder.Plugins.AddFromType<DocumentUploadPlugin>();
            skBuilder.Plugins.AddFromType<DocumentUploadSearchPlugin>();

            // RAG
            skBuilder.Services.AddSingleton<QdrantClient>(sp => new QdrantClient("localhost"));
            skBuilder.Services.AddQdrantVectorStore();
            skBuilder.Services.AddScoped(typeof(ITextManager), typeof(TextManager));
            skBuilder.Services.AddScoped(typeof(IRAGManager), typeof(RAGManager));


            // Build the kernel
            Kernel kernel = skBuilder.Build();

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            var textEmbeddingsGenerationService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

            return new SemanticKernelBuilderResult
            {
                AIServices = new AIServices
                {
                    ChatCompletionService = chatCompletionService,
                    TextEmbeddingGenerationService = textEmbeddingsGenerationService,
                    Kernel = kernel
                },
            };
        }

    }
}
