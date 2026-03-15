using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticSwamp.Shared
{
    /// <summary>
    /// Container class for application-wide enumerations.
    /// Grouping enums in a single class makes them easy to discover and import
    /// via the <c>static SemanticSwamp.Shared.Enums</c> using directive.
    /// </summary>
    public class Enums
    {
        /// <summary>
        /// Identifies the pre-loaded sample files that ship with the application
        /// under the <c>/SampleData/</c> directory.
        /// These are used by <c>UploadManager.GetTextFileSummaryFromPath</c> to let
        /// developers quickly test the summarisation pipeline without uploading a real file.
        /// </summary>
        public enum LocalFileTypes
        {
            /// <summary>A Wikipedia article about sports history (HTML file).</summary>
            SportsHistory,

            /// <summary>A plain-text file listing a top-5 movies ranking.</summary>
            Top5Movies,

            /// <summary>Project Gutenberg plain-text edition of Homer's The Odyssey.</summary>
            TheOdyssey
        }
    }
}
