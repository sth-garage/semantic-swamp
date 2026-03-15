using Microsoft.Extensions.Configuration;
using SemanticSwamp.Shared.Models;

namespace SemanticSwamp.Shared.Utility
{
    /// <summary>
    /// Reads application secrets from the .NET user-secrets store and maps them into
    /// a strongly typed <see cref="ConfigurationValues"/> object.
    /// <para>
    /// User secrets are a safe way to keep sensitive values (API keys, connection strings)
    /// out of source control during local development. They are stored outside the project
    /// directory and are never committed to Git.
    /// </para>
    /// <para>
    /// To initialise secrets for this project run the following commands from the solution root:
    /// <code>
    /// dotnet user-secrets set "LMStudio_ApiKey"    "&lt;your-key&gt;"
    /// dotnet user-secrets set "LMStudio_ApiUrl"    "http://127.0.0.1:1234/v1"
    /// dotnet user-secrets set "LMStudio_Model"     "&lt;model-id&gt;"
    /// dotnet user-secrets set "ConnectionString_SemanticSwamp" "Data Source=...;TrustServerCertificate=True"
    /// </code>
    /// </para>
    /// </summary>
    public class UserSecretManager
    {

        // dotnet user-secrets set "LMStudio_Model" "12345"
        // dotnet user-secrets set "ConnectionString_SemanticSwamp" "Data Source=127.0.0.1;Initial Catalog=SemanticSwamp;User Id=semanticSwampServiceLogin;Password=Testing777!!;TrustServerCertificate=True"
        /// <summary>
        /// Builds a <see cref="ConfigurationValues"/> instance by reading all expected keys from
        /// the supplied <see cref="IConfigurationRoot"/>.
        /// Returns an empty (default) <see cref="ConfigurationValues"/> if <paramref name="configurationRoot"/> is null,
        /// which prevents startup crashes when secrets have not yet been configured.
        /// </summary>
        /// <param name="configurationRoot">
        /// The configuration root built by the host; typically includes the user-secrets provider
        /// registered via <c>builder.Configuration.AddUserSecrets&lt;T&gt;()</c>.
        /// </param>
        /// <returns>A populated <see cref="ConfigurationValues"/> instance; values default to empty strings if keys are missing.</returns>
        public static ConfigurationValues GetSecrets(IConfigurationRoot? configurationRoot)
        {
            var result = new ConfigurationValues();

            if (configurationRoot != null)
            {

                result = new ConfigurationValues
                {

                    LMStudioSettings = new LMStudioSettings
                    {
                        // dotnet user-secrets set "LMStudio_ApiKey" "openai/gpt-oss-20b"
                        LMStudio_ApiKey = configurationRoot["LMStudio_ApiKey"] ?? "",

                        // dotnet user-secrets set "LMStudio_ApiUrl" "http://127.0.0.1:1234/v1"

                        LMStudio_ApiUrl = configurationRoot["LMStudio_ApiUrl"] ?? "",

                        // dotnet user-secrets set "LMStudio_Model" "openai/gpt-oss-20b"
                        LMStudio_Model = configurationRoot["LMStudio_Model"] ?? "",
                    },
                    ConnectionStrings = new ConnectionStrings
                    {
                        // dotnet user-secrets set "ConnectionString_SemanticSwamp" "Data Source=127.0.0.1;Initial Catalog=SemanticSwamp;User Id=semanticSwampServiceLogin;Password=Testing777!!;TrustServerCertificate=True;MultipleActiveResultSets=True"
                        ConnectionString_SemanticSwamp = configurationRoot["ConnectionString_SemanticSwamp"] ?? ""
                    }
                }; //
            }

            return result;
        }
    }
}
