using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace SemanticSwamp.Shared.Models
{


    public class SemanticKernelBuilderResult
    {
        public AIServices AIServices { get; set; } = new AIServices();
    }


    public class AIServices
    {
        public IChatCompletionService ChatCompletionService { get; set; }
        
        public ITextEmbeddingGenerationService TextEmbeddingGenerationService { get; set; }

        public Kernel Kernel { get; set; }
    }

    public class ModelAndKey
    {
        public string ModelId { get; set; }

        public string Key { get; set; }
    }

    public class AgentFromWeb
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public bool FinalReviewer { get; set; }
    }

    public class AgentPayload
    {
        public string Type { get; set; }

        public List<AgentFromWeb> Agents { get; set; } = new List<AgentFromWeb>();
    }

}
