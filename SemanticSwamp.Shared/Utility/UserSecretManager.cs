using Microsoft.Extensions.Configuration;
using SemanticSwamp.Shared.Models;

namespace SemanticSwamp.Shared.Utility
{
    public class UserSecretManager
    {

        // dotnet user-secrets set "LMStudio_Model" "12345"
        // dotnet user-secrets set "ConnectionString_SemanticSwamp" "Data Source=127.0.0.1;Initial Catalog=SemanticSwamp;User Id=semanticSwampServiceLogin;Password=Testing777!!;TrustServerCertificate=True"
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
