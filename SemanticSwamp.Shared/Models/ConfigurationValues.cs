namespace SemanticSwamp.Shared.Models;

/// <summary>
/// Root configuration object populated by UserSecretManager from .NET user secrets.
/// Passed as a singleton through DI so every service can access AI endpoints and
/// connection strings without reading IConfiguration directly.
/// </summary>
public class ConfigurationValues
{
    /// <summary>
    /// Which AI backend to use.
    /// Supported values: "LocalLLM" (LM Studio / OpenAI-compatible) or "AzureOpenAI".
    /// </summary>
    public string AIProvider { get; set; } = "LocalLLM";

    /// <summary>Settings for the LM Studio (local model) backend.</summary>
    public LMStudioSettings LMStudioSettings { get; set; } = new LMStudioSettings();

    /// <summary>Settings for Azure OpenAI (chat + embeddings).</summary>
    public AzureOpenAISettings AzureOpenAISettings { get; set; } = new AzureOpenAISettings();

    /// <summary>SQL Server connection strings used by EF Core.</summary>
    public ConnectionStrings ConnectionStrings { get; set; } = new ConnectionStrings();
}

/// <summary>
/// Connection details for Azure OpenAI.
/// Uses deployments (not model names) for both chat and embeddings.
/// </summary>
public class AzureOpenAISettings
{
    /// <summary>Azure OpenAI resource endpoint, e.g. https://&lt;resource&gt;.openai.azure.com/</summary>
    public string AzureOpenAI_Endpoint { get; set; } = "";

    /// <summary>API key for the Azure OpenAI resource.</summary>
    public string AzureOpenAI_ApiKey { get; set; } = "";

    /// <summary>Chat completion deployment name.</summary>
    public string AzureOpenAI_ChatDeployment { get; set; } = "";

    /// <summary>Text embedding deployment name.</summary>
    public string AzureOpenAI_EmbeddingDeployment { get; set; } = "";
}

/// <summary>
/// Connection details for an LM Studio or OpenAI-compatible local model endpoint.
/// LM Studio exposes an OpenAI-compatible REST API, so the same SK connector works
/// for both local models and cloud-hosted OpenAI endpoints — only these values change.
/// </summary>
public class LMStudioSettings
{
    /// <summary>API key sent in the Authorization header. For LM Studio this mirrors the model name.</summary>
    public string LMStudio_ApiKey { get; set; } = "";

    /// <summary>Model identifier (e.g. "openai/gpt-oss-20b" or "gpt-4.1" for OpenAI).</summary>
    public string LMStudio_Model { get; set; } = "";

    /// <summary>Base URL of the OpenAI-compatible endpoint (e.g. "http://127.0.0.1:1234/v1").</summary>
    public string LMStudio_ApiUrl { get; set; } = "";
}

/// <summary>
/// SQL Server connection strings used by the EF Core DbContext.
/// </summary>
public class ConnectionStrings
{
    /// <summary>Connection string for the main SemanticSwamp database.</summary>
    public string ConnectionString_SemanticSwamp { get; set; } = "";
}

