using System.Text.Json;

namespace SemanticSwamp
{
    /// <summary>
    /// Extension methods available on all objects, providing convenience helpers
    /// for development and debugging.
    /// </summary>
    public static class ObjectExtensions
    {
        // Cached options instance avoids re-allocating the options object on every call.
        private static readonly JsonSerializerOptions s_jsonOptionsCache = new() { WriteIndented = true };

        /// <summary>
        /// Serialises any object to an indented JSON string. Useful for quick debug logging
        /// (e.g. Console.WriteLine(someComplexObject.AsJson())) without needing to manually
        /// configure a JsonSerializer each time.
        /// </summary>
        public static string AsJson(this object obj)
        {
            return JsonSerializer.Serialize(obj, s_jsonOptionsCache);
        }
    }
}
