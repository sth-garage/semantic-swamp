namespace SemanticSwamp.Shared.Models;

public class ConfigurationValues
{
    public LMStudioSettings LMStudioSettings { get; set; } = new LMStudioSettings();

    public ConnectionStrings ConnectionStrings { get; set; } = new ConnectionStrings();
}

public class LMStudioSettings
{
    public string LMStudio_ApiKey { get; set; } = "";

    public string LMStudio_Model { get; set; } = "";

    public string LMStudio_ApiUrl { get; set; } = "";
}


public class ConnectionStrings
{
    public string ConnectionString_SemanticSwamp { get; set; } = "";
}

