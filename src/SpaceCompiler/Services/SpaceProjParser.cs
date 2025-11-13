using SpaceCompiler.Models;
using System.Text.RegularExpressions;

namespace SpaceCompiler.Services
{
    /// <summary>
    /// Parser for .spaceproj files using links-notation format
    /// Format example:
    /// Собаки: Files/File1.doc
    /// Кошки: Files/File2.doc
    /// Животные: File3.doc
    /// Животные: (Собаки Кошки)
    /// </summary>
    public class SpaceProjParser : ISpaceProjParser
    {
        private readonly ILogger<SpaceProjParser> _logger;

        public SpaceProjParser(ILogger<SpaceProjParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ProjectGraph> ParseAsync(string content)
        {
            _logger.LogInformation("Parsing .spaceproj content");

            var graph = new ProjectGraph
            {
                Metadata = new Dictionary<string, object>
                {
                    ["parsed_at"] = DateTime.UtcNow
                }
            };

            var nodeMap = new Dictionary<string, GraphNode>(StringComparer.OrdinalIgnoreCase);
            var links = new List<(string parent, List<string> children)>();

            // Parse each line
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                {
                    continue; // Skip empty lines and comments
                }

                ParseLine(trimmedLine, nodeMap, links);
            }

            // Build the graph structure
            BuildGraphStructure(nodeMap, links, graph);

            _logger.LogInformation("Parsed project graph with {NodeCount} nodes", nodeMap.Count);

            return await Task.FromResult(graph);
        }

        private void ParseLine(
            string line,
            Dictionary<string, GraphNode> nodeMap,
            List<(string parent, List<string> children)> links)
        {
            // Format: "Name: value" or "Name: (child1 child2 ...)"
            var colonIndex = line.IndexOf(':');
            if (colonIndex == -1)
            {
                _logger.LogWarning("Invalid line format (no colon): {Line}", line);
                return;
            }

            var name = line.Substring(0, colonIndex).Trim();
            var value = line.Substring(colonIndex + 1).Trim();

            // Ensure node exists
            if (!nodeMap.ContainsKey(name))
            {
                nodeMap[name] = new GraphNode
                {
                    Name = name,
                    Metadata = new Dictionary<string, object>()
                };
            }

            // Parse value
            if (value.StartsWith("(") && value.EndsWith(")"))
            {
                // This is a link to other nodes: Name: (Child1 Child2 ...)
                var childrenStr = value.Substring(1, value.Length - 2).Trim();
                var children = ParseChildren(childrenStr);

                // Record link for later processing
                links.Add((name, children));

                _logger.LogDebug("Found link: {Parent} -> ({Children})",
                    name, string.Join(", ", children));
            }
            else
            {
                // This is a file reference: Name: path/to/file
                var filePath = value;

                // Check if this is actually a reference to another node (no file extension or path)
                if (!filePath.Contains('/') && !filePath.Contains('\\') && !Path.HasExtension(filePath))
                {
                    // This looks like a node reference, treat it as a link
                    links.Add((name, new List<string> { filePath }));
                    _logger.LogDebug("Found node reference: {Parent} -> {Child}", name, filePath);
                }
                else
                {
                    // This is a file path
                    nodeMap[name].FilePath = filePath;
                    _logger.LogDebug("Found file mapping: {Name} -> {FilePath}", name, filePath);
                }
            }
        }

        private List<string> ParseChildren(string childrenStr)
        {
            // Split by spaces, but respect quoted strings
            var children = new List<string>();
            var matches = Regex.Matches(childrenStr, @"[^\s]+");

            foreach (Match match in matches)
            {
                children.Add(match.Value);
            }

            return children;
        }

        private void BuildGraphStructure(
            Dictionary<string, GraphNode> nodeMap,
            List<(string parent, List<string> children)> links,
            ProjectGraph graph)
        {
            // Process all links to build parent-child relationships
            foreach (var (parentName, childNames) in links)
            {
                if (!nodeMap.TryGetValue(parentName, out var parentNode))
                {
                    _logger.LogWarning("Parent node not found: {ParentName}", parentName);
                    continue;
                }

                foreach (var childName in childNames)
                {
                    if (!nodeMap.TryGetValue(childName, out var childNode))
                    {
                        // Create a placeholder node
                        childNode = new GraphNode
                        {
                            Name = childName,
                            Metadata = new Dictionary<string, object>
                            {
                                ["placeholder"] = true
                            }
                        };
                        nodeMap[childName] = childNode;
                        _logger.LogDebug("Created placeholder node: {ChildName}", childName);
                    }

                    // Add child to parent
                    if (!parentNode.Children.Contains(childNode))
                    {
                        parentNode.Children.Add(childNode);
                    }
                }
            }

            // Find root nodes (nodes that are not children of any other node)
            var allChildren = new HashSet<GraphNode>();
            foreach (var node in nodeMap.Values)
            {
                foreach (var child in node.Children)
                {
                    allChildren.Add(child);
                }
            }

            graph.Roots = nodeMap.Values
                .Where(node => !allChildren.Contains(node))
                .ToList();

            // If no explicit roots found, all top-level nodes are roots
            if (graph.Roots.Count == 0)
            {
                graph.Roots = nodeMap.Values.ToList();
            }

            _logger.LogInformation("Built graph with {RootCount} root nodes", graph.Roots.Count);
        }
    }
}
