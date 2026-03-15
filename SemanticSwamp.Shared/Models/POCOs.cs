using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace SemanticSwamp.Shared.Models
{
    /// <summary>
    /// The result returned by SKBuilder.BuildSemanticKernel. Bundles the three AI services
    /// that are extracted from the built Kernel and registered separately in DI so each can
    /// be injected individually where needed.
    /// </summary>
    public class SemanticKernelBuilderResult
    {
        public AIServices AIServices { get; set; } = new AIServices();
    }

    /// <summary>
    /// Holds the three core Semantic Kernel service instances extracted after Kernel.Build().
    /// These are registered as singletons in Program.cs because the underlying HTTP clients
    /// and model state are expensive to create and are safe to share across requests.
    /// </summary>
    public class AIServices
    {
        /// <summary>Sends chat messages to the configured LLM and returns responses.</summary>
        public IChatCompletionService ChatCompletionService { get; set; }

        /// <summary>Converts text into dense vector embeddings for semantic similarity search.</summary>
        public ITextEmbeddingGenerationService TextEmbeddingGenerationService { get; set; }

        /// <summary>
        /// The fully built SK Kernel with all plugins registered.
        /// Injected into ChatService so it can invoke kernel functions automatically
        /// during a chat turn (AutoInvokeKernelFunctions).
        /// </summary>
        public Kernel Kernel { get; set; }
    }

    /// <summary>
    /// Simple pair of a model identifier and its corresponding API key.
    /// Used in configuration helpers when constructing SK connectors programmatically.
    /// </summary>
    public class ModelAndKey
    {
        public string ModelId { get; set; }

        public string Key { get; set; }
    }

    /// <summary>
    /// Describes an AI agent as defined in the web UI, used when constructing
    /// multi-agent pipelines via AgentManager.
    /// </summary>
    public class AgentFromWeb
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>When true, this agent acts as the final reviewer in the pipeline.</summary>
        public bool FinalReviewer { get; set; }
    }

    /// <summary>
    /// Payload sent from the web client to configure a multi-agent invocation.
    /// Type describes the pipeline strategy; Agents lists the participants in order.
    /// </summary>
    public class AgentPayload
    {
        public string Type { get; set; }

        public List<AgentFromWeb> Agents { get; set; } = new List<AgentFromWeb>();
    }

    /// <summary>
    /// Holds the extracted text content of a single PDF page along with its page number.
    /// PDFManager produces a list of these which is then passed to the AI for
    /// faithful text reconstruction (re-ordering columns, joining split paragraphs).
    /// </summary>
    public class PDFText
    {
        /// <summary>Raw text extracted from the page by PdfPig's ContentOrderTextExtractor.</summary>
        public string Text { get; set; }

        /// <summary>1-based page number, used in the AI prompt so it can reference pages by number.</summary>
        public int PageNumber { get; set; }
    }
}
