using SpaceCompiler.Models;

namespace SpaceCompiler.Services
{
    /// <summary>
    /// Service for building AST (Abstract Syntax Tree) from tokens
    /// </summary>
    public class ParserService : IParserService
    {
        private readonly ILogger<ParserService> _logger;

        public ParserService(ILogger<ParserService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ParsedResource> BuildAstAsync(
            List<ContentFragment> fragments,
            string resourceId,
            string resourceType = "text",
            int maxBlockSize = 8000)
        {
            _logger.LogInformation("Building AST for resource {ResourceId} with {Count} fragments",
                resourceId, fragments.Count);

            var result = new ParsedResource
            {
                ResourceId = resourceId,
                ResourceType = resourceType,
                Metadata = new Dictionary<string, object>
                {
                    ["parsed_at"] = DateTime.UtcNow,
                    ["total_fragments"] = fragments.Count
                }
            };

            // Group fragments into blocks based on max block size
            result.Blocks = CreateBlocksFromFragments(fragments, maxBlockSize);

            result.Metadata["total_blocks"] = result.Blocks.Count;

            _logger.LogInformation("Built AST with {BlockCount} blocks from {FragmentCount} fragments",
                result.Blocks.Count, fragments.Count);

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Group fragments into blocks based on max block size
        /// </summary>
        private List<ContentBlock> CreateBlocksFromFragments(List<ContentFragment> fragments, int maxBlockSize)
        {
            var blocks = new List<ContentBlock>();
            var currentBlock = new ContentBlock
            {
                Order = 0,
                Type = "block",
                Fragments = new List<ContentFragment>()
            };

            int currentBlockSize = 0;

            foreach (var fragment in fragments)
            {
                var fragmentSize = fragment.Content.Length;

                // If adding this fragment exceeds max block size and current block is not empty, start new block
                if (currentBlockSize + fragmentSize > maxBlockSize && currentBlock.Fragments.Count > 0)
                {
                    // Finalize current block
                    currentBlock.Content = string.Join("\n\n", currentBlock.Fragments.Select(f => f.Content));
                    currentBlock.Metadata = new Dictionary<string, object>
                    {
                        ["fragment_count"] = currentBlock.Fragments.Count,
                        ["size"] = currentBlockSize
                    };
                    blocks.Add(currentBlock);

                    // Start new block
                    currentBlock = new ContentBlock
                    {
                        Order = blocks.Count,
                        Type = "block",
                        Fragments = new List<ContentFragment>()
                    };
                    currentBlockSize = 0;
                }

                // Add fragment to current block
                currentBlock.Fragments.Add(fragment);
                currentBlockSize += fragmentSize;
            }

            // Add final block if it has any fragments
            if (currentBlock.Fragments.Count > 0)
            {
                currentBlock.Content = string.Join("\n\n", currentBlock.Fragments.Select(f => f.Content));
                currentBlock.Metadata = new Dictionary<string, object>
                {
                    ["fragment_count"] = currentBlock.Fragments.Count,
                    ["size"] = currentBlockSize
                };
                blocks.Add(currentBlock);
            }

            _logger.LogInformation("Created {BlockCount} blocks from {FragmentCount} fragments",
                blocks.Count, fragments.Count);

            return blocks;
        }
    }
}
