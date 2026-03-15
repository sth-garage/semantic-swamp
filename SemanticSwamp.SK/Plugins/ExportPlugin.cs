using Microsoft.SemanticKernel;

namespace SemanticSwamp.Plugins
{
    /// <summary>
    /// A Semantic Kernel plugin that allows the AI to write text content to a file on the server.
    /// Useful for scenarios where the user asks the AI to save or export generated content
    /// (e.g., "save this summary to C:\output\result.txt").
    /// <para>
    /// Note: this plugin writes to the server's file system, so <paramref name="filePath"/> must
    /// be a path accessible by the server process. Use with caution in production environments.
    /// </para>
    /// </summary>
    public class ExportPlugin
    {
        /// <summary>
        /// Kernel function invoked when the AI needs to persist text to a file.
        /// Writes the provided text to the specified file path using <see cref="File.WriteAllText"/>,
        /// overwriting any existing content at that path.
        /// <para>
        /// Note: the method signature uses <c>async void</c> (fire-and-forget), which means exceptions
        /// from the write operation will not propagate to the caller. For production use, consider
        /// changing the return type to <c>Task</c> so failures can be observed and handled.
        /// </para>
        /// </summary>
        /// <param name="textToExport">The text content to write to the file.</param>
        /// <param name="filePath">The absolute file path on the server where the text will be written.</param>
        [KernelFunction("export_text")]
        public async void ExportText(string textToExport, string filePath)
        {
            File.WriteAllText(filePath, textToExport);
        }
    }
}
