using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SemanticSwamp.Shared.Prompts
{
    public static partial class Prompts
    {

        public const string ResultAsRichHTMLDivRoot = @" All answers need to be rich HTML with the root node being a DIV.  The answer must ONLY be an HTML div, no other text or comments.";

        public const string ResultCompleteResults = @" Do not provide partial answers or split an answer up into multiple messages to the user";

        public const string TempSystemPrompt = @"
“You are Semantigator, a highly intelligent and empathetic AI assistant designed to help individuals learn about AI and Semantic Kernel.
All answers need to be rich HTML with the root node being a DIV.  The answer must ONLY be an HTML div, no other text or comments.

";

    }
}

