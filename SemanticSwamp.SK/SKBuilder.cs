
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
using System.Collections;
using System.Data.SqlTypes;
using System.Net;
using System.Text;
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

            // Build the kernel
            Kernel kernel = skBuilder.Build();

            var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            var vectorStore = new QdrantVectorStore(new QdrantClient("localhost"), ownsClient: true);

            var collection = new QdrantCollection<ulong, TextEntry>(
                new QdrantClient("localhost"),
                "text",
                ownsClient: true);

            await collection.EnsureCollectionExistsAsync();


            //using HttpClient client = new();
            //string s = await client.GetStringAsync("https://devblogs.microsoft.com/dotnet/performance_improvements_in_net_7");
            //List<string> paragraphs =
            //    TextChunker.SplitPlainTextParagraphs(
            //        TextChunker.SplitPlainTextLines(
            //            WebUtility.HtmlDecode(Regex.Replace(s, @"<[^>]+>|&nbsp;", "")),
            //            128),
            //        1024);
            //for (int i = 0; i < paragraphs.Count; i++)
            //{ 
            //    var paragraph = paragraphs[i];

            //    await collection.UpsertAsync(new TextEntry
            //    {
            //        Id = idValue++,
            //        ArticleName = "Test",
            //        Text = paragraph,
            //        TextEmbedding = await textEmbed.GenerateEmbeddingAsync(paragraph)
            //    });
            //}





            //var ai = kernel.GetRequiredService<IChatCompletionService>();
            ChatHistory chat = new("You are an AI assistant that helps people find information.");
            StringBuilder builder = new();

            // User question & answer loop
            bool nextQuestion = true;
            while (nextQuestion)
            {
                //var question = AnsiConsole.Prompt(new TextPrompt<string>("[grey][[Optional]][/] Ask a Question: ").AllowEmpty());
                var vectorSearchOptions = new VectorSearchOptions<TextEntry>
                {
                    //Filter = r => r.ArticleName == "External Definitions"
                };
                builder.Clear();
                var question = "What are some security improvements in .NET?";
                var searchVector = (await embeddingGenerator.GenerateEmbeddingAsync(question));

                await foreach (var result in collection.SearchAsync(searchVector, 3, vectorSearchOptions))
                {
                    builder.AppendLine(result.Record.Text);
                }

                int contextToRemove = -1;
                if (builder.Length != 0)
                {
                    builder.Insert(0, "Here's some additional information: ");
                    contextToRemove = chat.Count;
                    chat.AddUserMessage(builder.ToString());
                }

                chat.AddUserMessage("What are some security improvements in .NET?");

                builder.Clear();
                await foreach (var message in chatCompletionService.GetStreamingChatMessageContentsAsync(chat))
                {
                    Console.Write(message);
                    builder.Append(message.Content);
                }
                Console.WriteLine();
                chat.AddAssistantMessage(builder.ToString());

                if (contextToRemove >= 0) chat.RemoveAt(contextToRemove);
                Console.WriteLine();
            }

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



    //public class FinanceInfo
    //{
    //    [VectorStoreKey]
    //    public Guid Key { get; set; } = Guid.NewGuid();

    //    [VectorStoreData]
    //    public string Text { get; set; } = string.Empty;

    //    // Note that the vector property is typed as a string, and
    //    // its value is derived from the Text property. The string
    //    // value will however be converted to a vector on upsert and
    //    // stored in the database as a vector.
    //    [VectorStoreVector(1536)]
    //    public string Embedding => this.Text;
    //}

    //public class Hotel
    //{
    //    [VectorStoreKey]
    //    public ulong HotelId { get; set; }

    //    [VectorStoreData(IsIndexed = true, StorageName = "hotel_name")]
    //    public string HotelName { get; set; }

    //    [VectorStoreData(IsFullTextIndexed = true, StorageName = "hotel_description")]
    //    public string Description { get; set; }

    //    [VectorStoreVector(384)]
    //    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }
    //}

    public class TextEntry
    {
        [VectorStoreKey]
        public ulong Id { get; set; }

        [VectorStoreData(IsIndexed = true)]
        public string ArticleName { get; set; }

        [VectorStoreData(IsFullTextIndexed = true)]
        public string Text { get; set; }

        [VectorStoreVector(384)]
        public ReadOnlyMemory<float>? TextEmbedding { get; set; }
    }
}
