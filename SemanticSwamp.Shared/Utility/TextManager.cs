using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticSwamp.Shared.Interfaces;
using SemanticSwamp.Shared.Prompts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SemanticSwamp.Shared.Utility
{
    public class TextManager : ITextManager
    {

        public string GetTextFileContent(string base64Data)
        {
            var result = "";

            var bytes = Convert.FromBase64String(base64Data);
            result = System.Text.Encoding.UTF8.GetString(bytes);
            return result;
        }

        public IEnumerable<string> Split(string str, int chunkSize)
        {
            return Enumerable.Range(0, str.Length / chunkSize)
                .Select(i => str.Substring(i * chunkSize, chunkSize));
        }

        public string GetBase64DataFromString(string value)
        {
            var result = "";

            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            result = Convert.ToBase64String(bytes);

            return result;
        }


        public async Task<string> GetBase64DataFromFile(IFormFile file)
        {
            var result = "";

            if (file == null || file.Length == 0)
            {
                result = "";
            }

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                var bytes = stream.ToArray();
                result = Convert.ToBase64String(bytes);
            }

            return result;

        }

        public List<string> GetChunks(string text, int chunkSize = 10000)
        {
            var result = Split(text, chunkSize).ToList();
            return result;
        }



    }
}
