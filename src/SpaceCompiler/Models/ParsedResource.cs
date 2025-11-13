namespace SpaceCompiler.Models
{
    /// <summary>
    /// Represents a parsed resource with its blocks and fragments (AST)
    /// </summary>
    public class ParsedResource
    {
        /// <summary>
        /// Resource identifier (filename, url, etc.)
        /// </summary>
        public string ResourceId { get; set; } = string.Empty;

        /// <summary>
        /// Resource type (text, json, doc, etc.)
        /// </summary>
        public string ResourceType { get; set; } = string.Empty;

        /// <summary>
        /// Original resource metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Parsed content blocks (AST intermediate level between resource and fragments)
        /// </summary>
        public List<ContentBlock> Blocks { get; set; } = new();
    }
}
