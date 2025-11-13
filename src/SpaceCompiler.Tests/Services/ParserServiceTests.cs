using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceCompiler.Models;
using SpaceCompiler.Services;
using Xunit;

namespace SpaceCompiler.Tests.Services
{
    public class ParserServiceTests
    {
        private readonly Mock<ILogger<ParserService>> _loggerMock;
        private readonly ParserService _service;

        public ParserServiceTests()
        {
            _loggerMock = new Mock<ILogger<ParserService>>();
            _service = new ParserService(_loggerMock.Object);
        }

        [Fact]
        public async Task BuildAstAsync_ShouldCreateParsedResource()
        {
            // Arrange
            var fragments = new List<ContentFragment>
            {
                new ContentFragment { Content = "Fragment 1", Order = 0, Type = "paragraph" },
                new ContentFragment { Content = "Fragment 2", Order = 1, Type = "paragraph" },
                new ContentFragment { Content = "Fragment 3", Order = 2, Type = "paragraph" }
            };

            // Act
            var result = await _service.BuildAstAsync(fragments, "test-resource", "text");

            // Assert
            result.Should().NotBeNull();
            result.ResourceId.Should().Be("test-resource");
            result.ResourceType.Should().Be("text");
            result.Blocks.Should().NotBeEmpty();
        }

        [Fact]
        public async Task BuildAstAsync_ShouldGroupFragmentsIntoBlocks()
        {
            // Arrange
            var fragments = new List<ContentFragment>();
            for (int i = 0; i < 10; i++)
            {
                fragments.Add(new ContentFragment
                {
                    Content = new string('a', 1000), // 1000 chars each
                    Order = i,
                    Type = "paragraph"
                });
            }

            // Act
            var result = await _service.BuildAstAsync(fragments, "test-resource", "text", maxBlockSize: 3000);

            // Assert
            result.Blocks.Should().HaveCountGreaterThan(1);
            result.Blocks.Sum(b => b.Fragments.Count).Should().Be(10);
        }

        [Fact]
        public async Task BuildAstAsync_ShouldSetBlockMetadata()
        {
            // Arrange
            var fragments = new List<ContentFragment>
            {
                new ContentFragment { Content = "Test content", Order = 0, Type = "paragraph" }
            };

            // Act
            var result = await _service.BuildAstAsync(fragments, "test-resource", "text");

            // Assert
            result.Blocks[0].Metadata.Should().ContainKey("fragment_count");
            result.Blocks[0].Metadata.Should().ContainKey("size");
        }
    }
}
