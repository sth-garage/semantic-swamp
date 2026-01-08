
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
    public class SKBuilder
    {
        public async Task<SemanticKernelBuilderResult> BuildSemanticKernel(ConfigurationValues configValues)
        {
            var modelId = configValues.LMStudioSettings.LMStudio_Model;
            var apiKey = configValues.LMStudioSettings.LMStudio_ApiKey;
            var apiUrl = configValues.LMStudioSettings.LMStudio_ApiUrl;

            HttpClient client = new HttpClient()
            {
                Timeout = new TimeSpan(0, 5, 0)
            };

            // Create a kernel with Azure OpenAI chat completion
            var skBuilder = Kernel.CreateBuilder().AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: modelId,
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
