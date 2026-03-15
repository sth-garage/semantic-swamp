using SemanticSwamp.DAL.EFModels;
using SemanticSwamp.Shared.Models.RAG;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticSwamp.Shared.Interfaces
{
    /// <summary>
    /// Defines the contract for the Retrieval-Augmented Generation (RAG) vector store manager.
    /// The primary implementation is <c>RAGManager</c> in the SK project, which uses Qdrant
    /// as the vector database and Semantic Kernel's <c>ITextEmbeddingGenerationService</c> to
    /// convert text chunks into dense vector embeddings for similarity search.
    /// </summary>
    public interface IRAGManager
    {
        /// <summary>
        /// Splits a document's text into semantic chunks, generates a vector embedding for each chunk,
        /// and upserts every chunk as a <see cref="DocumentUploadRAGEntry"/> into the Qdrant collection.
        /// A shared <c>IdTracker</c> row in the database provides monotonically increasing IDs so that
        /// Qdrant point IDs remain unique across restarts.
        /// </summary>
        /// <param name="documentUpload">The persisted document entity; its Base64Data (or overrideText) provides the raw content.</param>
        /// <param name="overrideText">
        /// When provided (e.g., for PDFs where text was extracted by <c>PDFManager</c>),
        /// this text is used instead of decoding <paramref name="documentUpload"/>.Base64Data.
        /// </param>
        Task UploadToRAG(DocumentUpload documentUpload, string overrideText = null);

        /// <summary>
        /// Performs a semantic similarity search against the Qdrant vector store.
        /// Before searching, the AI is asked which collection best matches the question;
        /// if a match is found the search is filtered to that collection only.
        /// Returns up to 30 of the most relevant document chunks.
        /// </summary>
        /// <param name="promptOrQuestion">The user's natural-language question or chat message.</param>
        /// <returns>A list of the most semantically similar <see cref="DocumentUploadRAGEntry"/> chunks.</returns>
        Task<List<DocumentUploadRAGEntry>> Search(string promptOrQuestion, string collection = "");
    }
}
