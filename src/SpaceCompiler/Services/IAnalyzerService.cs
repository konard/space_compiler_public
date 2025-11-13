using SpaceCompiler.Models;

namespace SpaceCompiler.Services
{
    /// <summary>
    /// Service for semantic analysis of AST nodes
    /// </summary>
    public interface IAnalyzerService
    {
        /// <summary>
        /// Perform semantic analysis on parsed resource
        /// </summary>
        /// <param name="parsedResource">Parsed resource with AST</param>
        /// <returns>Enhanced parsed resource with semantic metadata</returns>
        Task<ParsedResource> AnalyzeAsync(ParsedResource parsedResource);

        /// <summary>
        /// Analyze semantic patterns in text
        /// Uses statistical analysis and heuristics
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <returns>Semantic metadata</returns>
        Task<Dictionary<string, object>> AnalyzeSemanticPatternsAsync(string text);
    }
}
