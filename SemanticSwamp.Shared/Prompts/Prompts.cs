using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SemanticSwamp.Shared.Prompts
{
    /// <summary>
    /// Central repository of prompt strings and system instructions used when communicating
    /// with the AI chat completion service via Semantic Kernel.
    /// <para>
    /// Keeping prompts here (rather than scattered as inline strings) makes it easy to review,
    /// tune, and version-control the instructions the application sends to the model.
    /// </para>
    /// </summary>
    public static partial class Prompts
    {

        /// <summary>
        /// Instruction appended to prompts that require a fully-rendered HTML response.
        /// The UI renders AI responses as raw HTML inside a <c>@((MarkupString)message.Content)</c>
        /// expression, so the model must return a self-contained <c>&lt;div&gt;</c> with no
        /// surrounding prose or markdown code fences.
        /// </summary>
        public const string ResultAsRichHTMLDivRoot = @" All answers need to be rich HTML with the root node being a DIV.  The answer must ONLY be an HTML div, no other text or comments.";

        /// <summary>
        /// Instruction that tells the model not to truncate or paginate its answer.
        /// Prevents the common behaviour where a model says "here are the first N items..."
        /// when the full answer would be long.
        /// </summary>
        public const string ResultCompleteResults = @" Do not provide partial answers or split an answer up into multiple messages to the user";

        /// <summary>
        /// The system-level persona prompt injected at the start of every chat session.
        /// Establishes the AI as "Semantigator" — a helpful AI education assistant — and
        /// enforces the HTML-div-only output constraint so every reply can be rendered
        /// directly as markup in the Blazor chat UI.
        /// </summary>
        public const string TempSystemPrompt = "You are Semantigator, a highly intelligent and empathetic AI assistant designed to help individuals learn about AI and Semantic Kernel. " +
            "All answers need to be rich HTML with the root node being a DIV. The answer must ONLY be an HTML div, no other text or comments.";

        /// <summary>
        /// Prefix prompt sent before document text to instruct the model to produce a concise summary.
        /// The actual document content (in chunks) is appended as subsequent user messages
        /// so the total payload is broken up across the context window.
        /// </summary>
        public const string SummarizeText = @"Summarize the content of the following text: ";
    }
}
