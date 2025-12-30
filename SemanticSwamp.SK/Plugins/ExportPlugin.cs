using Microsoft.SemanticKernel;

namespace SemanticSwamp.Plugins
{
    public class ExportPlugin
    {
        [KernelFunction("export_text")]
        public async void ExportText(string textToExport, string filePath)
        {
            File.WriteAllText(filePath, textToExport);
        }
    }
}
