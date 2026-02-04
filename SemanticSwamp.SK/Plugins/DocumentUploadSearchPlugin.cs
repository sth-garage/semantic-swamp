
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.Interfaces;
using SemanticSwamp.Shared.Models;
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

        [KernelFunction("search_info_about_movie_odyssey")]
        [Description("Returns info about the 2026 movie The Odyssey by Christopher Nolan")]
        public async Task<List<DocumentUploadRAGEntry>> SearchForOdysseyInfo(string question)
        {
            var result = await _ragManager.Search(question);
            return result;
        }

        [KernelFunction("search_info_about_notepadpp_config")]
        [Description("Returns a list of info about configuring Notepad++")]
        public async Task<List<DocumentUploadRAGEntry>> SearchForNotepadPP(string question)
        {
            var result = await _ragManager.Search(question);
            return result;
        }
    }
}
