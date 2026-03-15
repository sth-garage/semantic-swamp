# Document Upload Pipeline

## Overview

When a user clicks **Upload File** in the Upload modal, the following sequence is triggered.
This entire flow is orchestrated by `UploadManager.ProcessUpload` in `SemanticSwamp.AppLogic`.

---

## Step-by-Step Flow

```
UploadModal.razor (Blazor)
    │
    │  FileUploadDTO (file + collectionId/Name + categoryId/Name + termIds + newTermNames)
    ▼
IFileManager.ProcessUpload()          [UploadManager — AppLogic]
    │
    ├─ 1. GetTerms()
    │       ├─ Existing terms → look up by ID from DB
    │       └─ New terms     → parse JSON array, INSERT new Term rows
    │
    ├─ 2. AddFileMetaData()
    │       └─ Read IFormFile bytes → Base64 encode → store on DocumentUpload entity
    │
    ├─ 3. SetCollection()
    │       ├─ New name provided? → INSERT new Collection
    │       └─ Existing ID?      → fetch from DB
    │
    ├─ 4. SetCategory()
    │       ├─ New name provided? → INSERT new Category
    │       └─ Existing ID?      → fetch from DB
    │
    ├─ 5. DB.DocumentUploads.Add() + SaveChangesAsync()
    │       └─ DocumentUpload row now exists with an auto-generated Id
    │
    ├─ 6. LinkTermsToDocumentUpload()
    │       └─ INSERT DocumentUploadTerm rows (one per term)
    │
    ├─ 7. PDF check
    │       └─ If FileName ends with ".pdf":
    │               ├─ PDFManager.GetContent(Base64Data)   [see below]
    │               └─ Re-encode extracted text back to Base64 for the summary step
    │
    ├─ 8. GetTextSummary(base64ForSummary)
    │       ├─ Decode Base64 → plain text
    │       ├─ Split into 10,000-char chunks
    │       ├─ Add each chunk as a ChatHistory user message
    │       └─ Call IChatCompletionService → return summary string
    │
    ├─ 9. RAGManager.UploadToRAG(documentUpload, overrideText?)
    │       └─ [See docs/05-rag-pipeline.md]
    │
    └─ 10. SaveChangesAsync()
            └─ Persist Summary + HasBeenProcessed = true
```

---

## PDF Text Extraction (PDFManager)

PDFs have a special sub-pipeline handled by `PDFManager` in `SemanticSwamp.AppLogic`:

```
Base64 PDF string
    │
    ▼
PDFManager.GetContent(base64Data)
    │
    ├─ Decode Base64 → byte[]
    │
    ├─ PdfPig: open PDF, iterate pages
    │       └─ ContentOrderTextExtractor.GetText(page) → raw page text
    │           (This preserves reading order better than word-order iteration)
    │
    ├─ Build List<PDFText> (one per page, with page number + raw text)
    │
    └─ GetContent(List<PDFText>)
            ├─ For each page: send to AI with column-correction prompt
            │       "PDF extractors output multi-column text interleaved.
            │        Reconstruct the correct reading order. Do NOT summarise —
            │        return ALL original text in the right order."
            └─ Return concatenated AI-corrected full-text string
```

The AI correction step is necessary because `ContentOrderTextExtractor` can still produce
interleaved text on complex multi-column layouts (e.g., academic papers, brochures).

---

## IBrowserFile → IFormFile Adapter

Blazor's file picker provides an `IBrowserFile` (browser-side stream). The upload pipeline
(`UploadManager`, `TextManager`) expects `IFormFile` (ASP.NET Core server-side abstraction).
The adapter is created in `UploadModal.razor`:

```csharp
// Copy browser stream into a server-side MemoryStream (max 10 MB)
var ms = new MemoryStream();
await _selectedFile.OpenReadStream(10_000_000).CopyToAsync(ms);
ms.Position = 0;

// Wrap in FormFile so downstream code sees a standard IFormFile
var formFile = new Microsoft.AspNetCore.Http.FormFile(
    ms, 0, ms.Length, "file", _selectedFile.Name)
{
    Headers = new HeaderDictionary(),
    ContentType = _selectedFile.ContentType
};
```

---

## New Term JSON Format

When a user types new term names in the Upload modal, they are staged as a `List<string>`
and serialized as a JSON array string for the DTO:

```csharp
// UploadModal.razor — serialise new terms
newTermNames = _newTermNames.Any()
    ? System.Text.Json.JsonSerializer.Serialize(_newTermNames)   // e.g. ["machine-learning","2024"]
    : ""
```

`UploadManager.GetTerms` deserialises this string, trims bracket/quote characters from each
element, and inserts a new `Term` entity for each name.

---

## Local File Smoke Test

The Upload modal has a **"Local file test"** section that bypasses the file-picker and runs
the summarisation pipeline against a pre-bundled sample file from the `SampleData/` directory.
This is intended for developers to quickly validate the AI connection is working.

Available sample files (mapped by `Enums.LocalFileTypes`):

| Enum Value | File | Content |
|---|---|---|
| `SportsHistory` | `DirtyBird-Wikipedia.html` | Wikipedia article about sports |
| `Top5Movies` | `top5movies.txt` | Short top-5 movies list |
| `TheOdyssey` | `pg1727_TheOdyssey.txt` | Full text of Homer's Odyssey (large!) |

The test calls `IFileManager.GetTextFileSummaryFromPath(LocalFileTypes)` which reads the file
from disk, Base64-encodes its bytes, and feeds them through the normal `GetTextSummary` path.
No DB writes or RAG indexing occur during a local test.
