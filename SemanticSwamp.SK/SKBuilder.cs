
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Text;
using Qdrant.Client;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.Shared.Models;
using SemanticSwamp.SK.Plugins;
using System.Data.SqlTypes;
using System.Net;
using System.Text.RegularExpressions;
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

            // Create a kernel with Azure OpenAI chat completion
            var skBuilder = Kernel.CreateBuilder().AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: modelId,
                endpoint: new Uri(apiUrl)
            ).AddLocalTextEmbeddingGeneration();

            skBuilder.Services.AddDbContext<SemanticSwampDBContext>(options =>
            {
                options.UseSqlServer(configValues.ConnectionStrings.ConnectionString_SemanticSwamp,
                    sqlServerOptions => sqlServerOptions.CommandTimeout(600));
            });

            skBuilder.Services.AddSingleton<ConfigurationValues>(configValues);


            // Plugins
            skBuilder.Plugins.AddFromType<DocumentUploadPlugin>();



            // Local RAG
            skBuilder.Services.AddSingleton<QdrantClient>(sp => new QdrantClient("localhost"));
            skBuilder.Services.AddQdrantVectorStore();
            //skBuilder.Services.AddLocalTextEmbeddingGeneration();

            // Build the kernel
            Kernel kernel = skBuilder.Build();


            var vectorStore = new QdrantVectorStore(new QdrantClient("localhost"), ownsClient: true);
            var textEmbed = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

            var collection = new QdrantCollection<ulong, Hotel>(
                new QdrantClient("localhost"),
                "hotel4",
                ownsClient: true);

            //var collection = new QdrantCollection<ulong, FinanceInfo>(
            //    new QdrantClient("localhost"),
            //    "finance3",
            //    ownsClient: true);

            await collection.EnsureCollectionExistsAsync();
            ReadOnlyMemory<float> searchEmbedding = await textEmbed.GenerateEmbeddingAsync("This is a description");

            //await collection.UpsertAsync(new FinanceInfo
            //{
            //    Key = Guid.NewGuid(),
            //    Text = "test"
            //});

            //var test = collection.SearchAsync<string>("What is the names?", 2);


            ulong idValue = 3;
            await collection.UpsertAsync(new Hotel
            {
                HotelId = idValue,
                HotelName = "Name",
                Description = "This is a description",
                DescriptionEmbedding = searchEmbedding
            });

            var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

            var searchVector = (await embeddingGenerator.GenerateEmbeddingAsync("test"));
            await collection.SearchAsync(searchVector, top: 20, new()
            {

            }).ForEachAsync(x =>
            {
                var blah = x.Record;
                var sto = 1;
            });

            //return resultRecords;


            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            return new SemanticKernelBuilderResult
            {
                AIServices = new AIServices
                {
                    ChatCompletionService = chatCompletionService,
                    Kernel = kernel
                },
            };
        }

    }



    public class FinanceInfo
    {
        [VectorStoreKey]
        public Guid Key { get; set; } = Guid.NewGuid();

        [VectorStoreData]
        public string Text { get; set; } = string.Empty;

        // Note that the vector property is typed as a string, and
        // its value is derived from the Text property. The string
        // value will however be converted to a vector on upsert and
        // stored in the database as a vector.
        [VectorStoreVector(1536)]
        public string Embedding => this.Text;
    }

    public class Hotel
    {
        [VectorStoreKey]
        public ulong HotelId { get; set; }

        [VectorStoreData(IsIndexed = true, StorageName = "hotel_name")]
        public string HotelName { get; set; }

        [VectorStoreData(IsFullTextIndexed = true, StorageName = "hotel_description")]
        public string Description { get; set; }

        [VectorStoreVector(384)]
        public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }
    }
}
