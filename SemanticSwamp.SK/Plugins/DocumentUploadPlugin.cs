
using SemanticSwamp.Shared.Models;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;
using SemanticSwamp.DAL.Context;
using SemanticSwamp.DAL.EFModels;

namespace SemanticSwamp.SK.Plugins
{
    /// <summary>
    /// A Semantic Kernel plugin that exposes document management operations as kernel functions.
    /// The AI can auto-invoke these functions when it determines that document data is needed
    /// to answer a user's question (e.g., "what files have been uploaded?").
    /// <para>
    /// Registered via <c>skBuilder.Plugins.AddFromType&lt;DocumentUploadPlugin&gt;()</c> in
    /// <c>SKBuilder</c>, which makes all <c>[KernelFunction]</c>-decorated methods available
    /// for automatic invocation by the Semantic Kernel planner.
    /// </para>
    /// </summary>
    public class DocumentUploadPlugin
    {
        private SemanticSwampDBContext _context;
        private ConfigurationValues _configValues;

        /// <summary>
        /// Initialises the plugin with DI-injected services.
        /// </summary>
        /// <param name="context">EF Core DB context for querying the <c>DocumentUploads</c> table.</param>
        /// <param name="configValues">Application configuration (available for potential future use).</param>
        public DocumentUploadPlugin(SemanticSwampDBContext context, ConfigurationValues configValues)
        {
            _context = context;
            _configValues = configValues;
        }

        /// <summary>
        /// Kernel function invoked when the AI needs to enumerate available uploaded documents.
        /// Returns only filenames (not content), so the AI can present a list to the user
        /// or choose a specific file to read with <see cref="GetDocumentUploadByFileName"/>.
        /// </summary>
        [KernelFunction("list_document_upload_filenames")]
        [Description("Returns a list of document upload filenames, can be used when asked about creating a list or seeing what document uploads are or are available.")]
        public async Task<List<string>> ListDocumentUploads()
        {
            var names = _context.DocumentUploads.Select(x => x.FileName).ToList();
            return names;
        }

        /// <summary>
        /// Kernel function invoked when the AI needs the full <see cref="DocumentUpload"/> entity for a
        /// specific file. The entity includes metadata such as the summary, collection, and category,
        /// which can be used to answer questions about a document without reading its full content.
        /// </summary>
        [KernelFunction("get_document_upload_by_filename")]
        [Description("Using a file name provided by the user, returns the Document Upload entity back")]
        public async Task<DocumentUpload> GetDocumentUploadByFileName(string fileName)
        {
            var documentUpload = _context.DocumentUploads.FirstOrDefault(x => x.FileName == fileName);
            return documentUpload;
        }

        /// <summary>
        /// Kernel function invoked when the AI needs to read the raw text content of a specific document.
        /// Decodes the Base64-encoded file content from the database back to a UTF-8 string.
        /// Useful when the AI needs to quote or reason over the full text of a document.
        /// </summary>
        [KernelFunction("read_file_by_doc_upload_id")]
        [Description("Using the id for a document upload, return the string that is in the file")]
        public async Task<string> GetDocumentUploadContentsByFileName(int documentUploadId)
        {
            var result = "";
            var documentUpload = _context.DocumentUploads.FirstOrDefault(x => x.Id == documentUploadId);

            if (documentUpload != null)
            {
                byte[] data = Convert.FromBase64String(documentUpload.Base64Data);
                result = System.Text.Encoding.UTF8.GetString(data);
            }

            return result;
        }

    }
}
