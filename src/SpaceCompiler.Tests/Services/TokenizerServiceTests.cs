using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceCompiler.Services;
using Xunit;

namespace SpaceCompiler.Tests.Services
{
    public class TokenizerServiceTests
    {
        private readonly Mock<ILogger<TokenizerService>> _loggerMock;
        private readonly TokenizerService _service;

        public TokenizerServiceTests()
        {
            _loggerMock = new Mock<ILogger<TokenizerService>>();
            _service = new TokenizerService(_loggerMock.Object);
        }

        [Fact]
        public async Task TokenizeAsync_WithTextContent_ShouldCreateFragments()
        {
            // Arrange
            var content = "This is a normal paragraph with enough content to meet the minimum length requirement for fragment creation.";

            // Act
            var result = await _service.TokenizeAsync(content, "text");

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result[0].Content.Should().Contain("normal paragraph");
            result[0].Type.Should().Be("paragraph");
            result[0].Order.Should().Be(0);
        }

        [Fact]
        public async Task TokenizeAsync_WithMultipleParagraphs_ShouldCreateMultipleFragments()
        {
            // Arrange
            var content = @"First paragraph with sufficient content to meet requirements.

Second paragraph that also has enough text to be considered valid.

Third paragraph continuing the pattern of adequate length.";

            // Act
            var result = await _service.TokenizeAsync(content, "text");

            // Assert
            result.Should().HaveCount(3);
            result[0].Content.Should().Contain("First paragraph");
            result[1].Content.Should().Contain("Second paragraph");
            result[2].Content.Should().Contain("Third paragraph");
            result[0].Order.Should().Be(0);
            result[1].Order.Should().Be(1);
            result[2].Order.Should().Be(2);
        }

        [Fact]
        public async Task TokenizeAsync_WithShortParagraphs_ShouldMergeThem()
        {
            // Arrange
            var content = @"Short one.

Short two.

Short three.";

            // Act
            var result = await _service.TokenizeAsync(content, "text");

            // Assert
            result.Should().HaveCount(1);
            result[0].Content.Should().Contain("Short one");
            result[0].Content.Should().Contain("Short two");
            result[0].Content.Should().Contain("Short three");
        }

        [Fact]
        public async Task TokenizeAsync_WithJsonContent_ShouldParseJsonStructure()
        {
            // Arrange
            var content = @"{
                ""name"": ""Test"",
                ""value"": 123,
                ""items"": [1, 2, 3]
            }";

            // Act
            var result = await _service.TokenizeAsync(content, "json");

            // Assert
            result.Should().NotBeEmpty();
            result.Should().Contain(f => f.Type == "json_object");
        }

        [Fact]
        public void GetSupportedContentTypes_ShouldReturnKnownTypes()
        {
            // Act
            var types = _service.GetSupportedContentTypes();

            // Assert
            types.Should().Contain("text");
            types.Should().Contain("json");
        }
    }
}
