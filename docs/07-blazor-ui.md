# Blazor UI

## Overview

The frontend is a **Blazor Server** application (`SemanticSwamp.Blazor`) using the
`InteractiveServer` render mode. The entire UI runs on the server; the browser communicates
with it via a persistent **SignalR** WebSocket connection. This means:

- No client-side JavaScript framework is needed.
- All C# state lives on the server — there is no serialisation between client and server.
- Each browser tab gets its own **SignalR circuit** with its own scoped service instances.

---

## Project Layout

```
SemanticSwamp.Blazor/
│
├── Program.cs                          ← Composition root. DI registrations, middleware, minimal APIs.
│
├── Components/
│   ├── App.razor                       ← Root component. Sets up the document shell (head, body).
│   ├── _Imports.razor                  ← Global @using directives for all components.
│   ├── Pages/
│   │   └── Home.razor                  ← The only page. Chat UI + navbar buttons.
│   └── Modals/
│       ├── UploadModal.razor           ← File upload modal (file picker, collection/category/terms).
│       └── DocumentsModal.razor        ← Document browser modal (table of uploads + download).
│
├── Models/
│   └── UiChatMessage.cs               ← View model for a single chat message (role, content, timestamp).
│
├── Services/
│   ├── ChatService.cs                  ← Scoped. Owns ChatHistory + UiChatMessage list for one circuit.
│   └── EntityService.cs               ← Scoped. Thin read-only facade over SemanticSwampDBContext.
│
└── wwwroot/
    ├── css/swamp.css                   ← All custom styles (chat bubbles, navbar, modals, animations).
    ├── js/swamp.js                     ← JS interop helpers (Bootstrap modal show/hide, scroll, file input).
    └── Images/                         ← Logo and decorative images.
```

---

## Pages

### `Home.razor` — The Only Page

The entire application lives on a single page. It is composed of three visual zones:

```
┌─────────────────────────────────────────────┐
│  Brand Header (logo + status pill)          │
├─────────────────────────────────────────────┤
│  Navbar (Upload / Documents buttons +       │
│          horizontal scrollable prompt chips)│
├─────────────────────────────────────────────┤
│                                             │
│  Chat Card                                  │
│  ┌─────────────────────────────────────┐    │
│  │  Message list (scrollable)          │    │
│  │  • Assistant messages (HTML markup) │    │
│  │  • User messages (plain text)       │    │
│  └─────────────────────────────────────┘    │
│  Input row (textarea + Send button)         │
└─────────────────────────────────────────────┘
```

**Key behaviours:**

- `OnInitialized` subscribes to `ChatService.OnStateChanged` → calls `InvokeAsync(StateHasChanged)`.
- `OnAfterRenderAsync(firstRender)` fires `ChatSvc.IntroduceAsync()` as a fire-and-forget Task
  so the page renders immediately before the AI intro response arrives.
- Pressing **Enter** (without Shift) in the textarea submits the message.
- The **Send** button and textarea are disabled while `ChatService.IsWaiting` is true.
- The **status pill** in the header reflects `ChatService.StatusKind` ("connecting", "thinking", "ready", "offline").

---

## Modals

### `UploadModal.razor`

A Bootstrap modal triggered from the navbar's **Upload** button via `uploadModal.ShowAsync()`.

**Form sections:**

| Section | Controls | Notes |
|---|---|---|
| 1. Pick a file | Hidden `<InputFile>` + visible "Choose File" button | JS interop (`triggerFileInput`) clicks the hidden input |
| 2. Where should it live? | Collection dropdown or new-name text input + Category dropdown or new-name text input | Toggle buttons switch between "pick existing" and "create new" modes |
| 3. Tag terms | Checkbox badges for existing terms + text input to add new terms | New terms are staged in-memory and serialised as JSON for the DTO |
| 4. Local file test | Dropdown of `LocalFileTypes` enum values | Selecting a value switches the button to "Test Local File" mode |

**State fields:**

```csharp
// Entity lists (populated by EntityService on ShowAsync)
List<Collection> _collections;
List<Category>   _categories;
List<Term>       _terms;
List<string>     _localFileTypes;

// Selected values
int    _selectedCollectionId;
int    _selectedCategoryId;
HashSet<int> _selectedTermIds;   // HashSet prevents duplicates

// "Create new" mode toggles
bool   _newCollectionMode;
bool   _newCategoryMode;
string _newCollectionName;
string _newCategoryName;

// New term staging
string       _newTermInput;      // text field buffer
List<string> _newTermNames;      // staged new terms (not yet in DB)

// Local test mode (computed property)
string _selectedLocalFile;
bool   _localTestMode => !string.IsNullOrEmpty(_selectedLocalFile);

// Upload state
IBrowserFile? _selectedFile;
bool          _isUploading;
string        _statusMessage;
bool          _isError;
string        _localFileResult;  // AI summary for local test result modal
```

### `DocumentsModal.razor`

A Bootstrap modal triggered from the navbar's **Documents** button.

Displays a table of all `DocumentUpload` records with:
- Filename, Collection, Category, upload date, processed status.
- Expandable row showing the AI-generated summary.
- **Download** button that calls `window.downloadFile('/api/download/{id}', fileName)` via JS interop,
  triggering a browser download via the `/api/download/{id}` minimal API endpoint.

---

## Services

### `ChatService` (Scoped)

Owns the entire chat session for one browser circuit.

| Property / Method | Purpose |
|---|---|
| `Messages` (`List<UiChatMessage>`) | All messages shown in the chat UI, chronologically. |
| `IsWaiting` | `true` while an AI call is in-flight. Disables the input. |
| `IsReady` | `true` after `IntroduceAsync` completes. Prevents sending before warmup. |
| `Status` | Human-readable text for the header status pill. |
| `StatusKind` | Machine-readable CSS key: `"connecting"`, `"thinking"`, `"ready"`, `"offline"`. |
| `OnStateChanged` | Event fired on every state change. `Home.razor` calls `InvokeAsync(StateHasChanged)` in its handler. |
| `IntroduceAsync()` | Sends a hidden "introduce yourself" prompt; adds the AI's greeting as the first message. |
| `SendAsync(message)` | Adds a user message, calls the SK kernel with streaming, streams tokens into a new assistant message. |

**Why `InvokeAsync(StateHasChanged)`?**
SK AI calls execute on background threads. Blazor's `StateHasChanged` must run on the
Blazor synchronisation context. `InvokeAsync` marshals the call back onto the right thread.

### `EntityService` (Scoped)

A thin, synchronous read-only facade over `SemanticSwampDBContext`.

| Method | Returns |
|---|---|
| `GetCollections()` | `List<Collection>` — all active collections |
| `GetCategories()` | `List<Category>` — all categories |
| `GetTerms()` | `List<Term>` — all terms |
| `GetDocumentUploads()` | `List<DocumentUpload>` with navigation properties included |
| `GetLocalFileTypes()` | `List<string>` — display names of `Enums.LocalFileTypes` values |

### `UiChatMessage` (Model)

View model for a single chat bubble.

| Property | Type | Notes |
|---|---|---|
| `Role` | `string` | `"user"` or `"assistant"` |
| `Content` | `string` | Message text. For assistant messages, this is **raw HTML**. |
| `Timestamp` | `DateTime` | When the message was created (UTC). |
| `IsStreaming` | `bool` | `true` while the assistant message is still receiving tokens. |

---

## JS Interop Helpers (`swamp.js`)

| JS Function | Called From | Purpose |
|---|---|---|
| `showBootstrapModal(id)` | `UploadModal`, `DocumentsModal` | Shows a Bootstrap modal by ID (`bootstrap.Modal.getOrCreateInstance(...).show()`) |
| `hideBootstrapModal(id)` | `UploadModal` (local test) | Hides a Bootstrap modal by ID |
| `triggerFileInput(id)` | `UploadModal` | Programmatically clicks a hidden `<input type="file">` |
| `scrollToBottom(id)` | `Home.razor` | Scrolls the chat message container to the bottom after a new message arrives |
| `downloadFile(url, fileName)` | `DocumentsModal` | Fetches a file URL and triggers a browser download via a temporary anchor element |

---

## SignalR Message Size

The default SignalR hub message size is 32 KB. Because file uploads pass Base64-encoded
content through the SignalR channel (via `IBrowserFile.OpenReadStream`), the limit is
increased to **10 MB** in `Program.cs`:

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(o => o.MaximumReceiveMessageSize = 10 * 1024 * 1024);
```
