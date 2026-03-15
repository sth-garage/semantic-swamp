# RAG Pipeline

## What Is RAG?

**Retrieval-Augmented Generation** (RAG) improves AI responses by giving the model access
to a relevant excerpt of your own documents at answer time, rather than relying solely on
its training data.

The flow has two phases:
1. **Indexing** — When a document is uploaded, its text is split into chunks, each chunk
   is converted to a vector embedding, and the vectors are stored in Qdrant.
2. **Retrieval** — When a user asks a question, the question is also embedded, the closest
   matching chunks are retrieved from Qdrant, and those chunks are supplied to the AI as
   context before it generates an answer.

In Semantic Swamp, the retrieval step is triggered automatically by the
`DocumentUploadSearchPlugin` SK plugin whenever the AI decides the user's question is
about uploaded document content.

---

## Indexing Flow (UploadToRAG)

Triggered at the end of `UploadManager.ProcessUpload`, after the document is saved to SQL Server.

```
DocumentUpload entity (Id, FileName, CollectionId, CategoryId, Terms, Base64Data)
    │
    ▼
RAGManager.UploadToRAG(documentUpload, overrideText?)
    │
    ├─ Determine text to index:
    │       overrideText != null? → use it (PDF-extracted, AI-corrected text)
    │       otherwise             → decode DocumentUpload.Base64Data to plain text
    │
    ├─ GetChunks(text)                          [TextChunker — 2-pass]
    │       Pass 1: SplitPlainTextLines(128)    → lines of ≤128 tokens
    │       Pass 2: SplitPlainTextParagraphs(1024) → paragraphs of ≤1024 tokens
    │       Result: List<string> of paragraph-sized chunks
    │
    ├─ For each chunk:
    │       ├─ Fetch IdTracker from DB, increment LastIdUsed → unique Qdrant point ID
    │       │
    │       ├─ ITextEmbeddingGenerationService.GenerateEmbeddingAsync(chunk)
    │       │       → ReadOnlyMemory<float> (384 dimensions, local model)
    │       │
    │       └─ Upsert DocumentUploadRAGEntry to Qdrant:
    │               Id              = IdTracker.LastIdUsed (ulong)
    │               DocumentUploadId = documentUpload.Id
    │               CollectionId    = documentUpload.CollectionId
    │               CategoryId      = documentUpload.CategoryId
    │               FileName        = documentUpload.FileName
    │               Terms           = [list of term IDs]
    │               Text            = raw chunk text
    │               ChunkIndex      = 0, 1, 2 …
    │               Embedding       = 384-dim float vector
    │
    └─ SaveChangesAsync() → persist updated IdTracker
```

### Why IdTracker?

Qdrant requires globally unique `ulong` point IDs within a collection. The `IdTracker`
table holds the last ID issued so that IDs are unique across application restarts without
querying Qdrant for its current maximum ID. Each chunk increments the counter before
upserting.

### TextChunker Settings (128 / 1024 tokens)

| Setting | Value | Reason |
|---|---|---|
| Line token limit | 128 | Keeps lines short enough to respect sentence boundaries |
| Paragraph token limit | 1,024 | Groups lines into coherent retrieval units |
| Embedding model | Local 384-dim | No API cost; runs on server CPU |

Smaller chunks improve retrieval precision (less noise per result).
Larger chunks provide more context per returned chunk.
The 128/1024 split balances both.

---

## Search Flow (RAGManager.Search)

Triggered by `DocumentUploadSearchPlugin.SearchUploadedDocuments(question)` during an AI
chat turn.

```
User question (string)
    │
    ▼
RAGManager.Search(promptOrQuestion)
    │
    ├─ GetCollectionIdFromQuestion(question)
    │       ├─ Fetch all Collection names from DB
    │       ├─ Send to AI: "Given these collections: [X, Y, Z],
    │       │              which best captures this question? Answer with the name only."
    │       ├─ Clean AI response (strip markdown, trim whitespace)
    │       ├─ Find matching Collection by name → return CollectionId
    │       └─ No match found → return -1 (search all collections)
    │
    ├─ Embed the question:
    │       ITextEmbeddingGenerationService.GenerateEmbeddingAsync(question) → 384-dim vector
    │
    ├─ Qdrant vector search:
    │       Collection: "DocumentUpload"
    │       Vector: question embedding
    │       Filter: CollectionId == matchedId  (omitted if -1)
    │       Top: 30 nearest neighbours
    │
    └─ Return List<DocumentUploadRAGEntry>   (the 30 most similar chunks)
```

The returned chunks are passed back to the AI as context in the `[KernelFunction]` return
value. Semantic Kernel serialises them and injects them into the model's next prompt so the
AI can reason over the retrieved text when formulating its answer.

---

## Qdrant Collection Name

All document chunks are stored in a single Qdrant collection named **`"DocumentUpload"`**
(defined as a constant in `RAGManager._ragCollectionName`).

Scoping searches to a specific user collection is done via a **metadata filter** on the
`CollectionId` field (not by separate Qdrant collections), which keeps the architecture
simple — one Qdrant collection for everything.

---

## DocumentUploadRAGEntry Schema

Each vector point stored in Qdrant is an instance of `DocumentUploadRAGEntry`:

| Property | Qdrant Role | Indexed? |
|---|---|---|
| `Id` | Point ID (`ulong`) | — |
| `DocumentUploadId` | Payload field | ✅ |
| `CategoryId` | Payload field | ✅ |
| `CollectionId` | Payload field | ✅ (used as search filter) |
| `FileName` | Payload field | ✅ |
| `Terms` | Payload field (`List<int>`) | ✅ |
| `Text` | Payload field | Full-text indexed |
| `ChunkIndex` | Payload field | ❌ |
| `Embedding` | Dense vector (384-dim) | — (used for similarity) |
