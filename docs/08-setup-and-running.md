# Setup and Running Locally

## Prerequisites

Before running Semantic Swamp, you need four things installed and running:

| Requirement | Version | Notes |
|---|---|---|
| **.NET SDK** | 10.0+ | `dotnet --version` to check |
| **SQL Server** | 2019+ (or LocalDB) | Any edition works; LocalDB is fine for development |
| **LM Studio** | Latest | Free desktop app. Downloads and serves local LLMs. |
| **Qdrant** | Latest | Vector database. Easiest to run via Docker. |

---

## 1. Set Up SQL Server

### Create the Database

Run the following SQL against your SQL Server instance to create the schema:

```sql
CREATE DATABASE SemanticSwamp;
GO

USE SemanticSwamp;
GO

CREATE TABLE Collections (
    Id   INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(2000) NOT NULL
);

CREATE TABLE Categories (
    Id   INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(2000) NOT NULL
);

CREATE TABLE Terms (
    Id   INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(2000) NOT NULL
);

CREATE TABLE DocumentUploads (
    Id               INT IDENTITY(1,1) CONSTRAINT PK_Uploads PRIMARY KEY,
    FileName         VARCHAR(500) NOT NULL,
    Base64Data       VARCHAR(MAX),
    Summary          VARCHAR(MAX),
    CreatedOn        DATETIME NOT NULL CONSTRAINT DF_Uploads_CreatedOn DEFAULT (GETDATE()),
    IsActive         BIT NOT NULL CONSTRAINT DF_DocumentUploads_IsActive DEFAULT (1),
    HasBeenProcessed BIT NOT NULL DEFAULT (0),
    CollectionId     INT NOT NULL,
    CategoryId       INT NOT NULL,
    CONSTRAINT FK_DocumentUploads_Collections FOREIGN KEY (CollectionId) REFERENCES Collections(Id),
    CONSTRAINT FK_DocumentUploads_Categories  FOREIGN KEY (CategoryId)  REFERENCES Categories(Id)
);

CREATE TABLE DocumentUploadTerms (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    DocumentUploadId INT NOT NULL,
    TermId           INT NOT NULL,
    CONSTRAINT FK_DocumentUploadTerms_DocumentUploads FOREIGN KEY (DocumentUploadId) REFERENCES DocumentUploads(Id),
    CONSTRAINT FK_DocumentUploadTerms_Terms           FOREIGN KEY (TermId)           REFERENCES Terms(Id)
);

CREATE TABLE IdTracker (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    LastIdUsed  BIGINT NOT NULL DEFAULT (0)
);

-- Seed the IdTracker with a single row (required — the app expects exactly one row)
INSERT INTO IdTracker (LastIdUsed) VALUES (0);
```

### Create a Login (Optional)

If you want a dedicated SQL login instead of using Windows Authentication:

```sql
CREATE LOGIN semanticSwampServiceLogin WITH PASSWORD = 'YourPasswordHere';
CREATE USER  semanticSwampServiceLogin FOR LOGIN semanticSwampServiceLogin;
ALTER ROLE db_owner ADD MEMBER semanticSwampServiceLogin;
```

---

## 2. Start Qdrant

The easiest way to run Qdrant locally is via Docker:

```bash
docker pull qdrant/qdrant
docker run -d --name qdrant -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

- Port `6333` — Qdrant HTTP REST API
- Port `6334` — Qdrant gRPC (used by the .NET `QdrantClient`)

Verify it is running: [http://localhost:6333/dashboard](http://localhost:6333/dashboard)

The Qdrant collection named **`DocumentUpload`** is created automatically by Semantic Kernel
the first time a document is uploaded.

---

## 3. Start LM Studio

1. Download and install [LM Studio](https://lmstudio.ai/).
2. Download a model (any OpenAI-compatible model — `llama`, `mistral`, `phi`, etc.).
3. Go to **"Local Server"** (the `<->` icon in the left sidebar).
4. Load your chosen model and click **Start Server**.
5. Note the server URL (default: `http://127.0.0.1:1234`) and the model ID shown in the UI.

> **Tip:** Models with 7B+ parameters give better quality summaries. For local dev, a
> quantised 7B model (Q4_K_M) offers a good speed/quality balance.

---

## 4. Configure User Secrets

The application uses [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
to keep sensitive values out of source control. Run these commands from the
`SemanticSwamp.Blazor` project directory:

```powershell
cd SemanticSwamp.Blazor

dotnet user-secrets set "LMStudio_ApiKey"                   "lm-studio"
dotnet user-secrets set "LMStudio_ApiUrl"                   "http://127.0.0.1:1234/v1"
dotnet user-secrets set "LMStudio_Model"                    "your-model-id-here"
dotnet user-secrets set "ConnectionString_SemanticSwamp"    "Data Source=127.0.0.1;Initial Catalog=SemanticSwamp;User Id=semanticSwampServiceLogin;Password=YourPasswordHere;TrustServerCertificate=True"
```

> **Notes:**
> - `LMStudio_ApiKey` can be any non-empty string when targeting LM Studio (it ignores the key value).
> - `LMStudio_Model` must match the model ID shown in LM Studio's server tab (e.g. `"llama-3.2-3b-instruct"`).
> - If you use Windows Authentication for SQL Server, the connection string is:
>   `"Data Source=.;Initial Catalog=SemanticSwamp;Integrated Security=True;TrustServerCertificate=True"`

---

## 5. Build and Run

Open the solution in Visual Studio 2022+ (or VS 2026 Insiders) and set
**`SemanticSwamp.Blazor`** as the startup project, then press **F5**.

Or from the command line:

```powershell
cd SemanticSwamp.Blazor
dotnet run
```

The app will be available at `https://localhost:5001` (or the port shown in the terminal output).

---

## Startup Sequence

When the application starts, `Program.cs` performs the following steps in order:

1. Build the ASP.NET Core host with Blazor + SignalR.
2. Read user secrets into `ConfigurationValues`.
3. Call `SKBuilder.BuildSemanticKernel(configValues)` — this:
   - Connects to LM Studio (no actual call is made yet; just configures the HTTP client).
   - Loads the local embedding model into memory (SmartComponents).
   - Registers SK plugins in the kernel.
4. Register all DI services (DbContext, QdrantClient, SK services, AppLogic services).
5. Build the middleware pipeline.
6. Start listening for HTTP/WebSocket connections.

The first browser connection will trigger `ChatService.IntroduceAsync()`, which makes the
first real call to LM Studio. If LM Studio is not running, the status pill will show
**"Offline"** and an error message will appear in the chat.

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---|---|---|
| Status pill shows "Offline" | LM Studio server not running or wrong URL/port | Start LM Studio server; verify `LMStudio_ApiUrl` in user secrets |
| Upload hangs indefinitely | LM Studio is processing slowly (large doc) | Wait; the HTTP timeout is 2 hours. Consider using a smaller/faster model. |
| "Cannot find collection" Qdrant error | Qdrant not running | Start Qdrant: `docker start qdrant` |
| EF Core connection error at startup | SQL Server not reachable | Verify connection string; ensure SQL Server is running |
| SignalR disconnects during large upload | Message size too large | Check that `MaximumReceiveMessageSize = 10MB` is set in `Program.cs` (it should be already) |
| AI returns raw HTML tags instead of rendered content | Model ignoring system prompt | Try a more instruction-following model; check that `TempSystemPrompt` is being sent |
