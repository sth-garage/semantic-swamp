//using Google.Apis.Auth.AspNetCore3;
//using Google.Apis.Auth.OAuth2;
//using Google.Apis.Drive.v3;
//using Google.Apis.PeopleService.v1;
//using Google.Apis.Services;
//using Memory;
//using Microsoft.Extensions.AI;
//using Microsoft.SemanticKernel;
//using Microsoft.SemanticKernel.Connectors.Qdrant;
//using Qdrant.Client;
//using SemanticKernelWebClient.Models;

//namespace SemanticKernelWebClient.SK.SKQuickTesting.PluginTests
//{
//    public class RagTestPlugin
//    {
//        public RagTestPlugin()
//        {
//        }

//        [KernelFunction("ttttttest_rag_plugin_now")]
//        public async Task<string> TestRag(Kernel kernel)
//        {


//            GoogleCredential credential;

//            //string[] Scopes = { "https://www.googleapis.com/auth/androidworkzerotouchemm" };


//            var scopes = new[] { PeopleServiceService.ScopeConstants.UserinfoProfile, PeopleServiceService.ScopeConstants.UserinfoEmail, DriveService.ScopeConstants.DriveReadonly };

//            string ApplicationName = "Zero-touch Enrollment .NET Quickstart";

//            // Authenticate using the service account key
//            credential = GoogleCredential.FromFile(@"C:\Users\sholt\Downloads\postmanapp-467400-10b0b37f2616.json")
//                .CreateScoped(scopes);//.CreateWithUser("sholt1234@gmail.com");

//            // Create a zero-touch enrollment API service endpoint.
//            //var service = new AndroidProvisioningPartnerService(new BaseClientService.Initializer
//            //{
//            //    HttpClientInitializer = credential,
//            //    ApplicationName = ApplicationName
//            //});

//            //var test = StorageClient.Create(credential);

//            //var service2 = new PeopleServiceService(new BaseClientService.Initializer
//            //{
//            //    HttpClientInitializer = credential,
//            //    ApplicationName = ApplicationName
//            //});
//            //var files = service2.People;
//            //var test4 = files.Get(@"people/me");
//            ////test.Fields = "names";
//            //test4.PersonFields = "names";

//            //var res = await test4.ExecuteAsync();


//            //DirectoryService directoryService = new DirectoryService(new BaseClientService.Initializer
//            //{
//            //    HttpClientInitializer = credential,
//            //    ApplicationName = ApplicationName
//            //});

//            //var t1 = directoryService.

//            DriveService driveService = new DriveService(new BaseClientService.Initializer
//            {
//                HttpClientInitializer = credential,
//                ApplicationName = ApplicationName
//            });

//            var temp1 = driveService.Files;
//            var temp2 = temp1.List();
//            var temp3 = await temp2.ExecuteAsync();

//            var stop = 1;








//            Object obj = null;
//            var auth = kernel.Data.TryGetValue("auth", out obj);
//            IGoogleAuthProvider provider = obj as IGoogleAuthProvider;
//            //app.Services.GetRequiredService<IGoogleAuthProvider>();


//            GoogleCredential cred = await provider.GetCredentialAsync();


//            //            //var service = new PeopleServiceService(new BaseClientService.Initializer
//            //            //{
//            //            //    HttpClientInitializer = cred
//            //            //});
//            //            //var files = service.People;
//            //            //var test = files.Get(@"people/me");
//            //            ////test.Fields = "names";
//            //            //test.PersonFields = "names";

//            //            //var res = await test.ExecuteAsync();

//            // [FromServices] IGoogleAuthProvider auth

//            var embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

//            //var vectorStore = kernel.GetRequiredService<QdrantVectorStore>();


//            //HttpClient client = new HttpClient();
//            //var blah = await client.GetAsync("https://en.wikipedia.org/wiki/Novel");
//            //var blah2 = await blah.Content.ReadAsStringAsync();

//            //HtmlDocument doc = new HtmlDocument();
//            //doc.LoadHtml(blah2);

//            ////var query = $"div[@id='content']";
//            //HtmlNode node = doc.GetElementbyId("content");
//            //string content = node.InnerHtml;









//            var vectorStore = new QdrantVectorStore(
//               new QdrantClient("localhost"),
//               ownsClient: true,
//               new QdrantVectorStoreOptions
//               {
//                   EmbeddingGenerator = embeddingGenerator
//               });


//            var collection = vectorStore.GetCollection<Guid, FinanceInfo>("finances");
//            await collection.EnsureCollectionExistsAsync();

//            // Embeddings for the search is automatically generated on search.
//            var searchResult = collection.SearchAsync(
//                "What is my budget for 2024?",
//                top: 1);

//            // Output the matching result.
//            await foreach (var result in searchResult)
//            {
//                Console.WriteLine($"Key: {result.Record.Key}, Text: {result.Record.Text}");

//                return result.Record.Text;
//            }

//            return null;

//            //return search
//            //var stop = 1;
//        }


//        [KernelFunction("rag_db_test")]
//        public async Task<string> TestRagFromQDrantDB(Kernel kernel)
//        {
//            var embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

//            var vectorStore = new QdrantVectorStore(
//               new QdrantClient("localhost"),
//               ownsClient: true,
//               new QdrantVectorStoreOptions
//               {
//                   EmbeddingGenerator = embeddingGenerator
//               });

//            VectorProcessor test = new VectorProcessor(vectorStore, embeddingGenerator);
//            //var result = await test.IngestDataAndSearchAsync("skglossaryWithoutDI", () => Guid.NewGuid());

//            // Create the common processor that works for any vector store.
//            var processor = new VectorProcessor(vectorStore, embeddingGenerator);

//            // Run the process and pass a key generator function to it, to generate unique record keys.
//            // The key generator function is required, since different vector stores may require different key types.
//            // E.g. Qdrant supports Guid and ulong keys, but others may support strings only.
//            //var seOut = await processor.IngestDataAndSearchAdvancedAsync("shipexectest", () => Guid.NewGuid(), "ShipExec", "General", reply.Content);
//            //var testout = 1;



//            var seOut = await processor.Search("shipexectest", () => Guid.NewGuid(), "What is ShipExec?");
//            return seOut.First().Record.Definition;
//        }
//    }
//}
