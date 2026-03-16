# Blazor Primer (Blazor Server)

This document is a general-purpose primer on how Blazor Server works. It is not specific to Semantic Swamp, but it explains the key concepts used throughout the solution.

## 1. What Blazor is

Blazor is a .NET web UI framework that lets you build interactive user interfaces using:

- Razor components (`.razor` files)
- C# for state and event handling
- HTML + CSS for rendering

Blazor has two common hosting models:

- **Blazor Server**: Components execute on the server. The browser receives UI diffs over SignalR.
- **Blazor WebAssembly**: Components execute in the browser via WebAssembly.

Semantic Swamp uses **Blazor Server**.

## 2. Blazor Server execution model

### 2.1 Circuits

In Blazor Server, each connected browser tab/session establishes a **SignalR circuit**.

A circuit is effectively:

- A persistent connection (WebSocket when possible)
- A server-side component tree instance
- Per-circuit state stored in server memory

Implications:

- UI state is kept on the server.
- If the circuit disconnects and cannot be reconnected, state is lost unless you persist it.
- Long-running operations must be managed carefully so the circuit stays responsive.

### 2.2 Rendering and UI diffs

Blazor does not send raw HTML for every update. Instead:

- Components render to a render tree on the server
- Blazor computes a diff between old/new render trees
- The diff is sent to the browser over SignalR

This is why Blazor Server can feel like a SPA without building a JavaScript SPA.

### 2.3 Component lifecycle

Key lifecycle methods:

- `OnInitialized` / `OnInitializedAsync`
  - Initialization logic; safe place to subscribe to service events.
- `OnParametersSet` / `OnParametersSetAsync`
  - Called when parameters change.
- `OnAfterRender` / `OnAfterRenderAsync`
  - Called after the component has rendered.
  - Used for JS interop that needs DOM to exist.

In many Blazor Server apps, you kick off background work on first render:

- `OnAfterRenderAsync(firstRender: true)`

That pattern is used in Semantic Swamp to send the initial “introduce yourself” call.

## 3. Dependency Injection lifetimes (important in Blazor Server)

ASP.NET Core DI lifetimes:

- **Singleton**: one instance per app process
- **Scoped**: one instance per scope
- **Transient**: new instance per resolution

In Blazor Server, scoped services are **scoped to the circuit** (not just a single HTTP request).

Best practices:

- Use **scoped** services for per-user/per-tab state (chat history, UI state machines).
- Use **singleton** services for expensive stateless services (clients, caches, configuration objects).
- Avoid injecting `DbContext` directly into components.
  - Prefer a scoped service/facade that owns the `DbContext` interactions.

## 4. Handling async work and UI updates

### 4.1 StateHasChanged threading

If your service raises events from background threads, your component should use:

- `InvokeAsync(StateHasChanged)`

This marshals back onto the Blazor synchronization context.

### 4.2 Avoid blocking the circuit

Blocking work on the circuit thread will freeze UI updates.

Guidelines:

- Use `async/await` for I/O.
- Prefer background operations where appropriate.
- For CPU-heavy work, consider offloading to a worker service or separate process.

## 5. JavaScript interop

Blazor Server supports calling JavaScript and receiving results:

- `IJSRuntime.InvokeVoidAsync("fn", args...)`
- `IJSRuntime.InvokeAsync<T>("fn", args...)`

Use cases in Semantic Swamp include:

- showing/hiding Bootstrap modals
- auto-scrolling the chat area
- triggering a hidden file input
- initiating a download

JS interop is typically done in `OnAfterRenderAsync` or in event handlers.

## 6. File uploads in Blazor Server

Blazor provides `<InputFile>` which exposes an `IBrowserFile`. Common pattern:

1. User selects file (`IBrowserFile`)
2. The server reads the stream (with max size limits)
3. If your downstream logic expects `IFormFile`, you can adapt by copying to a `MemoryStream` and wrapping a `FormFile`

Because Blazor Server transfers data over SignalR, large files require increasing:

- SignalR hub message size (`MaximumReceiveMessageSize`)

## 7. Why Blazor Server is a good fit for Semantic Swamp

This solution benefits from Blazor Server because:

- all AI and database work naturally runs on the server
- large file uploads are easier to handle server-side
- sensitive configuration (connection strings, API keys) stays on the server
- you can integrate with .NET libraries (EF Core, Semantic Kernel, Qdrant client) without client-side constraints
