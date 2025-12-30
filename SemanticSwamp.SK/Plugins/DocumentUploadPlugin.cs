
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
    public class DocumentUploadPlugin
    {
        private SemanticSwampDBContext _context;
        private ConfigurationValues _configValues;

        public DocumentUploadPlugin(SemanticSwampDBContext context, ConfigurationValues configValues)
        {
            _context = context;
            _configValues = configValues;
        }

        [KernelFunction("list_document_upload_filenames")]
        [Description("Returns a list of document upload filenames, can be used when asked about creating a list or seeing what document uploads are or are available.")]
        public async Task<List<string>> ListDocumentUploads()
        {
            var names = _context.DocumentUploads.Select(x => x.FileName).ToList();
            return names;
        }

        [KernelFunction("get_document_upload_by_filename")]
        [Description("Using a file name provided by the user, returns the Document Upload entity back")]
        public async Task<DocumentUpload> GetDocumentUploadByFileName(string fileName)
        {
            var documentUpload = _context.DocumentUploads.FirstOrDefault(x => x.FileName == fileName);
            return documentUpload;
        }

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
