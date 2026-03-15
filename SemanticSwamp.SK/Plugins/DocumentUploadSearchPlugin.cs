using Microsoft.SemanticKernel;
using SemanticSwamp.Shared.Interfaces;
using SemanticSwamp.Shared.Models.RAG;
using System.ComponentModel;

namespace SemanticSwamp.SK.Plugins
{
    /// <summary>
    /// A Semantic Kernel plugin that exposes semantic document search as a kernel function.
    /// When the AI determines a user's question requires searching uploaded document content,
    /// it invokes <see cref="SearchUploadedDocuments"/> which delegates to <see cref="IRAGManager.Search"/>
    /// to perform a Qdrant vector similarity search and return the most relevant document chunks.
    /// <para>
    /// Registered via <c>skBuilder.Plugins.AddFromType&lt;DocumentUploadSearchPlugin&gt;()</c>.
    /// </para>
    /// </summary>
    public class DocumentUploadSearchPlugin
    {
        // Note: context, configValues, and kernel were previously injected but are no longer
        // needed here now that all RAG operations are delegated entirely to IRAGManager.
        private IRAGManager _ragManager;

        /// <summary>
        /// Initialises the plugin with the RAG manager needed for vector search.
        /// </summary>
        /// <param name="ragManager">The RAG manager that handles Qdrant vector search operations.</param>
        public DocumentUploadSearchPlugin(IRAGManager ragManager)
        {
            _ragManager = ragManager;
        }

        /// <summary>
        /// Kernel function that performs a semantic similarity search over all indexed document chunks.
        /// The AI invokes this function automatically when a user asks a question that likely requires
        /// knowledge from uploaded documents (e.g., "what does the uploaded report say about X?").
        /// Results are returned as a list of <see cref="DocumentUploadRAGEntry"/> objects which the
        /// AI uses as context to formulate its answer.
        /// </summary>
        [KernelFunction("search_uploaded_documents")]
        [Description("Searches uploaded documents for information relevant to the given question or topic.")]
        public async Task<List<DocumentUploadRAGEntry>> SearchUploadedDocuments(string question)
        {
            return await _ragManager.Search(question);
        }

    }
}
