namespace SpaceCompiler.Models
{
    /// <summary>
    /// Represents a graph node in the project structure
    /// </summary>
    public class GraphNode
    {
        /// <summary>
        /// Node name/identifier
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// File path if this node represents a file
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Child nodes
        /// </summary>
        public List<GraphNode> Children { get; set; } = new();

        /// <summary>
        /// Parsed content if this is a file node
        /// </summary>
        public ParsedResource? ParsedContent { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Represents the complete project graph structure
    /// </summary>
    public class ProjectGraph
    {
        /// <summary>
        /// Root nodes of the graph
        /// </summary>
        public List<GraphNode> Roots { get; set; } = new();

        /// <summary>
        /// Project metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
