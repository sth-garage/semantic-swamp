
using Microsoft.SemanticKernel;
using SemanticSwamp.Shared.Interfaces;
using SemanticSwamp.Shared.Models.RAG;
using System.ComponentModel;

namespace SemanticSwamp.SK.Plugins
{
    public class DocumentUploadSearchPlugin
    {
        //private SemanticSwampDBContext _context;
        //private ConfigurationValues _configValues;
        //private Kernel _kernel;
        private IRAGManager _ragManager;

        public DocumentUploadSearchPlugin(IRAGManager ragManager)
        {
            //_context = context;
            //_configValues = configValues;
            //_kernel = kernel;
            _ragManager = ragManager;
        }

        [KernelFunction("search_uploaded_documents")]
        [Description("Searches uploaded documents for information relevant to the given question or topic.")]
        public async Task<List<DocumentUploadRAGEntry>> SearchUploadedDocuments(string question)
        {
            return await _ragManager.Search(question);
        }
    }
}
