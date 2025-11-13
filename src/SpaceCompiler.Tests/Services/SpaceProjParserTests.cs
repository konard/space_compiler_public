using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceCompiler.Services;
using Xunit;

namespace SpaceCompiler.Tests.Services
{
    public class SpaceProjParserTests
    {
        private readonly Mock<ILogger<SpaceProjParser>> _loggerMock;
        private readonly SpaceProjParser _parser;

        public SpaceProjParserTests()
        {
            _loggerMock = new Mock<ILogger<SpaceProjParser>>();
            _parser = new SpaceProjParser(_loggerMock.Object);
        }

        [Fact]
        public async Task ParseAsync_WithSimpleFileMapping_ShouldCreateNodes()
        {
            // Arrange
            var content = @"
Собаки: Files/File1.doc
Кошки: Files/File2.doc
Животные: File3.doc
";

            // Act
            var result = await _parser.ParseAsync(content);

            // Assert
            result.Should().NotBeNull();
            result.Roots.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ParseAsync_WithHierarchicalStructure_ShouldBuildGraph()
        {
            // Arrange
            var content = @"
Собаки: Files/File1.doc
Кошки: Files/File2.doc
Животные: File3.doc
Животные: (Собаки Кошки)
";

            // Act
            var result = await _parser.ParseAsync(content);

            // Assert
            result.Should().NotBeNull();
            result.Roots.Should().NotBeEmpty();

            // Find the Животные node
            var animalsNode = result.Roots.FirstOrDefault(n => n.Name == "Животные");
            animalsNode.Should().NotBeNull();
            animalsNode!.Children.Should().HaveCount(2);
        }

        [Fact]
        public async Task ParseAsync_ShouldSkipEmptyLines()
        {
            // Arrange
            var content = @"
Node1: file1.txt

Node2: file2.txt

";

            // Act
            var result = await _parser.ParseAsync(content);

            // Assert
            result.Should().NotBeNull();
            result.Roots.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ParseAsync_ShouldSkipComments()
        {
            // Arrange
            var content = @"
# This is a comment
Node1: file1.txt
# Another comment
Node2: file2.txt
";

            // Act
            var result = await _parser.ParseAsync(content);

            // Assert
            result.Should().NotBeNull();
        }
    }
}
