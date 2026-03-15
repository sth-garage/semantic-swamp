# Data Model

## SQL Server Schema

The EF Core entities are scaffolded from an existing SQL Server database.
The `SemanticSwampDBContext` is a `partial` class — EF scaffold can regenerate
the `OnModelCreating` configuration without overwriting hand-written extensions.

### Entity Relationship Diagram

```
┌──────────────┐       ┌──────────────────────┐       ┌──────────────┐
│  Collection  │       │    DocumentUpload    │       │   Category   │
│──────────────│       │──────────────────────│       │──────────────│
│ Id (PK)      │◄──────│ CollectionId (FK)    │──────►│ Id (PK)      │
│ Name         │  1:N  │ CategoryId (FK)      │  N:1  │ Name         │
└──────────────┘       │ Id (PK)              │       └──────────────┘
                       │ FileName             │
                       │ Base64Data           │
                       │ Summary              │
                       │ CreatedOn            │
                       │ IsActive             │
                       │ HasBeenProcessed     │
                       └──────────┬───────────┘
                                  │ 1:N
                                  ▼
                       ┌──────────────────────┐       ┌──────────────┐
                       │  DocumentUploadTerm  │       │     Term     │
                       │──────────────────────│       │──────────────│
                       │ Id (PK)              │──────►│ Id (PK)      │
                       │ DocumentUploadId (FK)│  N:1  │ Name         │
                       │ TermId (FK)          │       └──────────────┘
                       └──────────────────────┘

┌──────────────┐
│  IdTracker   │
│──────────────│
│ Id (PK)      │  ← Single-row table. Stores the last Qdrant point ID issued.
│ LastIdUsed   │
└──────────────┘
```

---

## Entity Descriptions

### `Collection`

Top-level grouping for documents. Examples: "Project Alpha", "Research Papers", "Legal Docs".

- Documents are filtered by collection during RAG search (the AI picks the most relevant
  collection before running the vector query to reduce noise).
- Created on-the-fly from the Upload modal if the user types a new collection name.

### `Category`

Finer-grained classification within a collection. Examples: "Lecture Notes", "Meeting Minutes".

- Stored on `DocumentUpload` as a FK. Available as a future filter on RAG search.
- Created on-the-fly from the Upload modal if the user types a new category name.

### `Term`

Keyword / concept tag applied to a document. Examples: "machine-learning", "Q3-2024", "confidential".

- Many-to-many with `DocumentUpload` via the `DocumentUploadTerm` join table.
- Existing terms are selected via checkboxes in the Upload modal.
- New terms can be typed inline; they are created in the database when the upload is submitted.
- Term IDs are stored in `DocumentUploadRAGEntry.Terms` so they are available for
  future term-scoped vector searches.

### `DocumentUpload`

The central entity representing an uploaded file.

| Column | Type | Notes |
|---|---|---|
| `Id` | `int` (PK) | Auto-generated. |
| `FileName` | `varchar(500)` | Original filename from the browser. |
| `Base64Data` | `varchar(max)` | Full file content encoded as Base64. |
| `Summary` | `varchar(max)` | AI-generated summary (plain text). |
| `CreatedOn` | `datetime` | Defaulted to `GETDATE()` by the database. |
| `IsActive` | `bit` | Soft-delete flag. Defaults to `true`. |
| `HasBeenProcessed` | `bit` | `true` once RAG vectorisation completes. |
| `CollectionId` | `int` (FK) | Collection this document belongs to. |
| `CategoryId` | `int` (FK) | Category this document belongs to. |

### `DocumentUploadTerm` (Join Table)

Implements the many-to-many relationship between `DocumentUpload` and `Term`.
Each row links one upload to one term. Both FKs use `ClientSetNull` delete behaviour —
deleting a document or term will not cascade-delete these rows in the database; EF
nullifies the FK on the client side instead.

### `IdTracker`

A single-row table (`ToTable("IdTracker")`) that holds the last Qdrant point ID issued
by `RAGManager.UploadToRAG`. Each call increments `LastIdUsed` before upserting chunks,
ensuring point IDs are globally unique across restarts without querying Qdrant for the
current maximum.

---

## DbContext Registration

```csharp
// Program.cs — scoped so each Blazor circuit gets its own change-tracked instance
builder.Services.AddDbContext<SemanticSwampDBContext>(options =>
{
    options.UseSqlServer(
        configValues.ConnectionStrings.ConnectionString_SemanticSwamp,
        sqlOpts => sqlOpts.CommandTimeout(6000)  // 6000s for long embedding operations
    );
});
```

The `CommandTimeout` is intentionally long because the PDF text extraction and summarisation
steps can take several minutes when using a slow local LLM.
