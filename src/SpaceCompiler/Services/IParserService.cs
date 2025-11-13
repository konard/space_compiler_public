using SpaceCompiler.Models;

namespace SpaceCompiler.Services
{
    /// <summary>
    /// Service for building AST (Abstract Syntax Tree) from tokens
    /// </summary>
    public interface IParserService
    {
        /// <summary>
        /// Build AST tree from fragments (tokens)
        /// </summary>
        /// <param name="fragments">List of content fragments</param>
        /// <param name="resourceId">Resource identifier</param>
        /// <param name="resourceType">Type of resource</param>
        /// <param name="maxBlockSize">Maximum block size for grouping</param>
        /// <returns>Parsed resource with AST structure</returns>
        Task<ParsedResource> BuildAstAsync(
            List<ContentFragment> fragments,
            string resourceId,
            string resourceType = "text",
            int maxBlockSize = 8000);
    }
}
