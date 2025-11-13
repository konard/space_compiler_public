using SpaceCompiler.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpaceCompiler.Services
{
    /// <summary>
    /// Service for tokenizing text into fragments
    /// Migrated from space_db_public parsers
    /// </summary>
    public class TokenizerService : ITokenizerService
    {
        private readonly ILogger<TokenizerService> _logger;
        private readonly int _minParagraphLength;
        private readonly int _maxParagraphLength;

        public TokenizerService(
            ILogger<TokenizerService> logger,
            int minParagraphLength = 50,
            int maxParagraphLength = 2000)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _minParagraphLength = minParagraphLength;
            _maxParagraphLength = maxParagraphLength;
        }

        public async Task<List<ContentFragment>> TokenizeAsync(string content, string contentType = "text")
        {
            _logger.LogInformation("Tokenizing content of type: {ContentType}, length: {Length}",
                contentType, content.Length);

            return contentType.ToLowerInvariant() switch
            {
                "text" or "txt" => await TokenizeTextAsync(content),
                "json" => await TokenizeJsonAsync(content),
                _ => await TokenizeTextAsync(content) // Default to text
            };
        }

        public IEnumerable<string> GetSupportedContentTypes()
        {
            return new[] { "text", "txt", "json" };
        }

        /// <summary>
        /// Tokenize plain text into paragraphs/sentences
        /// </summary>
        private async Task<List<ContentFragment>> TokenizeTextAsync(string content)
        {
            var fragments = new List<ContentFragment>();

            // Split by double newlines (paragraph separator)
            var rawParagraphs = Regex.Split(content, @"\n\s*\n|\r\n\s*\r\n")
                .Select(p => NormalizeText(p))
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            int order = 0;
            var paragraphBuffer = new List<string>();

            foreach (var paragraph in rawParagraphs)
            {
                // If paragraph is short, add to buffer
                if (paragraph.Length < _minParagraphLength)
                {
                    _logger.LogDebug("Adding short paragraph to buffer: {Length} chars", paragraph.Length);
                    paragraphBuffer.Add(paragraph);

                    // Check if buffered content is now long enough
                    var bufferedContent = string.Join("\n\n", paragraphBuffer);
                    if (bufferedContent.Length >= _minParagraphLength)
                    {
                        // Process buffered content
                        if (bufferedContent.Length > _maxParagraphLength)
                        {
                            var chunks = await SplitLongParagraphAsync(bufferedContent);
                            foreach (var chunk in chunks)
                            {
                                fragments.Add(CreateFragment(chunk, order++));
                            }
                        }
                        else
                        {
                            fragments.Add(CreateFragment(bufferedContent, order++));
                        }
                        paragraphBuffer.Clear();
                    }
                    continue;
                }

                // Flush buffer before processing long paragraph
                if (paragraphBuffer.Count > 0)
                {
                    var bufferedContent = string.Join("\n\n", paragraphBuffer);
                    fragments.Add(CreateFragment(bufferedContent, order++));
                    paragraphBuffer.Clear();
                }

                // Split long paragraphs into chunks
                if (paragraph.Length > _maxParagraphLength)
                {
                    var chunks = await SplitLongParagraphAsync(paragraph);
                    foreach (var chunk in chunks)
                    {
                        fragments.Add(CreateFragment(chunk, order++));
                    }
                }
                else
                {
                    fragments.Add(CreateFragment(paragraph, order++));
                }
            }

            // Flush any remaining buffered content
            if (paragraphBuffer.Count > 0)
            {
                var bufferedContent = string.Join("\n\n", paragraphBuffer);
                fragments.Add(CreateFragment(bufferedContent, order++));
                _logger.LogDebug("Flushed final buffer with {Count} short paragraphs", paragraphBuffer.Count);
            }

            _logger.LogInformation("Tokenized text into {Count} fragments", fragments.Count);
            return fragments;
        }

        /// <summary>
        /// Tokenize JSON into hierarchical fragments
        /// </summary>
        private async Task<List<ContentFragment>> TokenizeJsonAsync(string content)
        {
            var fragments = new List<ContentFragment>();
            int order = 0;

            try
            {
                using var document = JsonDocument.Parse(content);
                TokenizeJsonElement(document.RootElement, "root", fragments, ref order, 0, null);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse JSON content");
                // Fall back to treating as text
                return await TokenizeTextAsync(content);
            }

            _logger.LogInformation("Tokenized JSON into {Count} fragments", fragments.Count);
            return fragments;
        }

        private void TokenizeJsonElement(
            JsonElement element,
            string path,
            List<ContentFragment> fragments,
            ref int order,
            int depth,
            string? parentKey)
        {
            if (depth > 10) // Max depth limit
            {
                _logger.LogWarning("Max depth reached at path {Path}", path);
                return;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    TokenizeJsonObject(element, path, fragments, ref order, depth, parentKey);
                    break;

                case JsonValueKind.Array:
                    TokenizeJsonArray(element, path, fragments, ref order, depth, parentKey);
                    break;

                case JsonValueKind.String:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    var value = GetValueString(element);
                    if (element.ValueKind == JsonValueKind.String && value.Length > 20)
                    {
                        fragments.Add(new ContentFragment
                        {
                            Content = value,
                            Type = "json_value",
                            Order = order++,
                            ParentKey = parentKey,
                            Metadata = new Dictionary<string, object>
                            {
                                ["path"] = path,
                                ["value_type"] = "string",
                                ["length"] = value.Length
                            }
                        });
                    }
                    break;
            }
        }

        private void TokenizeJsonObject(
            JsonElement element,
            string path,
            List<ContentFragment> fragments,
            ref int order,
            int depth,
            string? parentKey)
        {
            var properties = new List<string>();

            foreach (var property in element.EnumerateObject())
            {
                var propertyPath = $"{path}.{property.Name}";
                properties.Add($"{property.Name}: {GetValuePreview(property.Value)}");

                if (ShouldTokenize(property.Value))
                {
                    TokenizeJsonElement(property.Value, propertyPath, fragments, ref order, depth + 1, path);
                }
            }

            if (properties.Count > 0)
            {
                var objectSummary = $"Object with {properties.Count} properties: " +
                    string.Join(", ", properties.Take(5));

                if (properties.Count > 5)
                {
                    objectSummary += $", ... ({properties.Count - 5} more)";
                }

                fragments.Add(new ContentFragment
                {
                    Content = objectSummary,
                    Type = "json_object",
                    Order = order++,
                    ParentKey = parentKey,
                    Metadata = new Dictionary<string, object>
                    {
                        ["path"] = path,
                        ["property_count"] = properties.Count,
                        ["depth"] = depth
                    }
                });
            }
        }

        private void TokenizeJsonArray(
            JsonElement element,
            string path,
            List<ContentFragment> fragments,
            ref int order,
            int depth,
            string? parentKey)
        {
            var arrayLength = element.GetArrayLength();
            var items = new List<string>();

            int index = 0;
            foreach (var item in element.EnumerateArray())
            {
                var itemPath = $"{path}[{index}]";
                items.Add(GetValuePreview(item));

                if (ShouldTokenize(item))
                {
                    TokenizeJsonElement(item, itemPath, fragments, ref order, depth + 1, path);
                }

                index++;
            }

            var arraySummary = $"Array with {arrayLength} items";
            if (items.Count > 0)
            {
                arraySummary += ": " + string.Join(", ", items.Take(3));
                if (items.Count > 3)
                {
                    arraySummary += $", ... ({items.Count - 3} more)";
                }
            }

            fragments.Add(new ContentFragment
            {
                Content = arraySummary,
                Type = "json_array",
                Order = order++,
                ParentKey = parentKey,
                Metadata = new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["array_length"] = arrayLength,
                    ["depth"] = depth
                }
            });
        }

        private ContentFragment CreateFragment(string content, int order)
        {
            return new ContentFragment
            {
                Content = content,
                Type = "paragraph",
                Order = order,
                Metadata = new Dictionary<string, object>
                {
                    ["length"] = content.Length,
                    ["word_count"] = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
                }
            };
        }

        private async Task<List<string>> SplitLongParagraphAsync(string paragraph)
        {
            var chunks = new List<string>();

            // Try to split by sentences first
            var sentences = Regex.Split(paragraph, @"(?<=[.!?])\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            var currentChunk = new List<string>();
            int currentLength = 0;

            foreach (var sentence in sentences)
            {
                if (currentLength + sentence.Length > _maxParagraphLength && currentChunk.Count > 0)
                {
                    chunks.Add(string.Join(" ", currentChunk));
                    currentChunk.Clear();
                    currentLength = 0;
                }

                currentChunk.Add(sentence);
                currentLength += sentence.Length;
            }

            if (currentChunk.Count > 0)
            {
                chunks.Add(string.Join(" ", currentChunk));
            }

            return await Task.FromResult(chunks);
        }

        private string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = Regex.Replace(text, @"\s+", " ");
            text = text.Trim();

            return text;
        }

        private bool ShouldTokenize(JsonElement element)
        {
            return element.ValueKind == JsonValueKind.Object ||
                   element.ValueKind == JsonValueKind.Array ||
                   (element.ValueKind == JsonValueKind.String && element.GetString()?.Length > 20);
        }

        private string GetValuePreview(JsonElement element, int maxLength = 50)
        {
            var value = GetValueString(element);
            if (value.Length > maxLength)
            {
                return value.Substring(0, maxLength) + "...";
            }
            return value;
        }

        private string GetValueString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? "",
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "null",
                JsonValueKind.Object => $"{{...}}",
                JsonValueKind.Array => $"[{element.GetArrayLength()} items]",
                _ => element.GetRawText()
            };
        }
    }
}
