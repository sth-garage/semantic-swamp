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
                        // openai/gpt-oss-20b
                        LMStudio_ApiKey = configurationRoot["LMStudio_ApiKey"] ?? "",

                        // http://127.0.0.1:1234/v1
                        LMStudio_ApiUrl = configurationRoot["LMStudio_ApiUrl"] ?? "",

                        // openai/gpt-oss-20b
                        LMStudio_Model = configurationRoot["LMStudio_Model"] ?? "",
                    },
                    ConnectionStrings = new ConnectionStrings
                    {
                        // Data Source=127.0.0.1;Initial Catalog=SemanticSwamp;User Id=semanticSwampServiceLogin;Password=Testing777!!;TrustServerCertificate=True
                        ConnectionString_SemanticSwamp = configurationRoot["ConnectionString_SemanticSwamp"] ?? ""
                    }
                };
            }

            return result;
        }
    }
}
