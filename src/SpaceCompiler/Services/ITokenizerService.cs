using SpaceCompiler.Models;

namespace SpaceCompiler.Services
{
    /// <summary>
    /// Service for tokenizing text into fragments
    /// </summary>
    public interface ITokenizerService
    {
        /// <summary>
        /// Tokenize text content into fragments
        /// </summary>
        /// <param name="content">Text content to tokenize</param>
        /// <param name="contentType">Type of content (text, json, etc.)</param>
        /// <returns>List of content fragments (tokens)</returns>
        Task<List<ContentFragment>> TokenizeAsync(string content, string contentType = "text");

        /// <summary>
        /// Get supported content types
        /// </summary>
        IEnumerable<string> GetSupportedContentTypes();
    }
}
