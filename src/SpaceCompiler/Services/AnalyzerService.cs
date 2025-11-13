using SpaceCompiler.Models;
using System.Text.RegularExpressions;

namespace SpaceCompiler.Services
{
    /// <summary>
    /// Service for semantic analysis of AST nodes
    /// Uses heuristics and statistical analysis to identify semantic patterns
    /// Can be extended with LLM for complex cases
    /// </summary>
    public class AnalyzerService : IAnalyzerService
    {
        private readonly ILogger<AnalyzerService> _logger;

        // Common stop words for semantic analysis
        private static readonly HashSet<string> StopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "up", "about", "into", "through", "during",
            "before", "after", "above", "below", "between", "under", "again", "further",
            "then", "once", "here", "there", "when", "where", "why", "how", "all",
            "both", "each", "few", "more", "most", "other", "some", "such", "no",
            "nor", "not", "only", "own", "same", "so", "than", "too", "very", "can",
            "will", "just", "should", "now", "is", "are", "was", "were", "be", "been",
            "being", "have", "has", "had", "do", "does", "did", "doing"
        };

        public AnalyzerService(ILogger<AnalyzerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ParsedResource> AnalyzeAsync(ParsedResource parsedResource)
        {
            _logger.LogInformation("Analyzing semantic patterns for resource {ResourceId}",
                parsedResource.ResourceId);

            // Analyze each block
            foreach (var block in parsedResource.Blocks)
            {
                var blockAnalysis = await AnalyzeSemanticPatternsAsync(block.Content);

                // Merge analysis results into block metadata
                if (block.Metadata == null)
                {
                    block.Metadata = new Dictionary<string, object>();
                }

                foreach (var kvp in blockAnalysis)
                {
                    block.Metadata[$"semantic_{kvp.Key}"] = kvp.Value;
                }

                // Analyze each fragment in the block
                foreach (var fragment in block.Fragments)
                {
                    var fragmentAnalysis = await AnalyzeSemanticPatternsAsync(fragment.Content);

                    if (fragment.Metadata == null)
                    {
                        fragment.Metadata = new Dictionary<string, object>();
                    }

                    foreach (var kvp in fragmentAnalysis)
                    {
                        fragment.Metadata[$"semantic_{kvp.Key}"] = kvp.Value;
                    }
                }
            }

            // Add overall semantic analysis to resource metadata
            if (parsedResource.Metadata == null)
            {
                parsedResource.Metadata = new Dictionary<string, object>();
            }

            var allContent = string.Join(" ", parsedResource.Blocks.Select(b => b.Content));
            var overallAnalysis = await AnalyzeSemanticPatternsAsync(allContent);

            foreach (var kvp in overallAnalysis)
            {
                parsedResource.Metadata[$"semantic_{kvp.Key}"] = kvp.Value;
            }

            _logger.LogInformation("Completed semantic analysis for resource {ResourceId}",
                parsedResource.ResourceId);

            return parsedResource;
        }

        public async Task<Dictionary<string, object>> AnalyzeSemanticPatternsAsync(string text)
        {
            var result = new Dictionary<string, object>();

            // Word frequency analysis
            var words = ExtractWords(text);
            var wordFrequency = CalculateWordFrequency(words);
            var topKeywords = wordFrequency
                .Where(kvp => !StopWords.Contains(kvp.Key))
                .OrderByDescending(kvp => kvp.Value)
                .Take(10)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            result["top_keywords"] = topKeywords;
            result["unique_word_count"] = wordFrequency.Count;
            result["total_word_count"] = words.Count;

            // Calculate lexical diversity (unique words / total words)
            var lexicalDiversity = wordFrequency.Count > 0
                ? (double)wordFrequency.Count / words.Count
                : 0;
            result["lexical_diversity"] = Math.Round(lexicalDiversity, 3);

            // Sentence analysis
            var sentences = ExtractSentences(text);
            result["sentence_count"] = sentences.Count;
            result["avg_sentence_length"] = sentences.Count > 0
                ? Math.Round((double)words.Count / sentences.Count, 1)
                : 0;

            // Detect content type/category using heuristics
            result["content_category"] = DetectContentCategory(text, topKeywords);

            // Detect semantic structure patterns
            result["has_questions"] = text.Contains("?");
            result["has_lists"] = Regex.IsMatch(text, @"^\s*[-*•]\s+", RegexOptions.Multiline);
            result["has_numbers"] = Regex.IsMatch(text, @"\d+");
            result["has_urls"] = Regex.IsMatch(text, @"https?://");

            // Average word length (complexity indicator)
            var avgWordLength = words.Count > 0
                ? Math.Round(words.Average(w => w.Length), 1)
                : 0;
            result["avg_word_length"] = avgWordLength;

            // Readability heuristic (simplified Flesch Reading Ease approximation)
            var avgSentenceLength = (double)(result["avg_sentence_length"] ?? 0);
            var readabilityScore = CalculateReadabilityScore(avgSentenceLength, avgWordLength);
            result["readability_score"] = readabilityScore;
            result["readability_level"] = GetReadabilityLevel(readabilityScore);

            return await Task.FromResult(result);
        }

        private List<string> ExtractWords(string text)
        {
            // Extract words (alphanumeric sequences)
            var words = Regex.Matches(text, @"\b[a-zA-Z]+\b")
                .Cast<Match>()
                .Select(m => m.Value.ToLowerInvariant())
                .Where(w => w.Length > 2) // Filter very short words
                .ToList();

            return words;
        }

        private Dictionary<string, int> CalculateWordFrequency(List<string> words)
        {
            var frequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var word in words)
            {
                if (frequency.ContainsKey(word))
                {
                    frequency[word]++;
                }
                else
                {
                    frequency[word] = 1;
                }
            }

            return frequency;
        }

        private List<string> ExtractSentences(string text)
        {
            // Split by sentence terminators
            var sentences = Regex.Split(text, @"[.!?]+\s+")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            return sentences;
        }

        private string DetectContentCategory(string text, Dictionary<string, int> topKeywords)
        {
            var textLower = text.ToLowerInvariant();

            // Technical/Programming
            if (topKeywords.Keys.Any(k => new[] { "function", "class", "method", "code", "data", "api", "system" }
                .Contains(k, StringComparer.OrdinalIgnoreCase)))
            {
                return "technical";
            }

            // Business/Professional
            if (topKeywords.Keys.Any(k => new[] { "company", "business", "market", "customer", "product", "service" }
                .Contains(k, StringComparer.OrdinalIgnoreCase)))
            {
                return "business";
            }

            // Academic/Research
            if (topKeywords.Keys.Any(k => new[] { "research", "study", "analysis", "theory", "hypothesis", "data" }
                .Contains(k, StringComparer.OrdinalIgnoreCase)))
            {
                return "academic";
            }

            // News/Informational
            if (topKeywords.Keys.Any(k => new[] { "reported", "announced", "according", "said", "stated" }
                .Contains(k, StringComparer.OrdinalIgnoreCase)))
            {
                return "news";
            }

            // Narrative/Story
            if (Regex.IsMatch(textLower, @"\b(once|there was|story|tale|character)\b"))
            {
                return "narrative";
            }

            return "general";
        }

        private double CalculateReadabilityScore(double avgSentenceLength, double avgWordLength)
        {
            // Simplified readability formula
            // Lower score = easier to read, higher score = more complex
            // Based on sentence length and word complexity
            var score = (avgSentenceLength * 0.5) + (avgWordLength * 2.0);
            return Math.Round(score, 1);
        }

        private string GetReadabilityLevel(double score)
        {
            return score switch
            {
                < 10 => "very_easy",
                < 15 => "easy",
                < 20 => "moderate",
                < 25 => "difficult",
                _ => "very_difficult"
            };
        }
    }
}
