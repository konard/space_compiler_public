# Space Compiler

A .NET 8 API for compiling documents into structured Abstract Syntax Trees (AST) with semantic analysis.

Comprehensive compiler of anything that contains data or semantics. Feature potential replacement of transformer (GPT) architecture.

## Overview

Space Compiler is a new methodology for document compilation, extracting the file parsing API from the [space_db_public](https://github.com/xlab2016/space_db_public) project. It provides a three-stage compilation pipeline:

1. **Tokenization** - Breaking text into fragments (tokens)
2. **Parsing** - Building AST trees from tokens
3. **Analysis** - Performing semantic analysis using heuristics and statistical methods

## Architecture

### Services

- **TokenizerService**: Splits text into fragments
  - Supports text and JSON content types
  - Handles paragraph merging and splitting
  - Preserves hierarchical structure for JSON

- **ParserService**: Builds AST from tokens
  - Groups fragments into blocks
  - Maintains order and relationships
  - Configurable block size limits

- **AnalyzerService**: Performs semantic analysis
  - Word frequency analysis
  - Keyword extraction
  - Readability scoring
  - Content categorization
  - Statistical pattern detection

- **SpaceProjParser**: Parses .spaceproj files
  - Based on [links-notation](https://github.com/link-foundation/links-notation)
  - Builds project graph structures
  - Supports hierarchical relationships

- **CompilationService**: Orchestrates the compilation pipeline
  - Single file compilation
  - Multi-file compilation
  - Project compilation from ZIP archives

## API Endpoints

### `/api/v1/compiler/compile/file`
Compile a single file.

**Request Body:**
```json
{
  "content": "File content here...",
  "fileName": "document.txt",
  "contentType": "text"
}
```

### `/api/v1/compiler/compile/files`
Compile multiple files as a unified tree.

**Request Body:**
```json
{
  "files": {
    "file1.txt": "Content 1...",
    "file2.txt": "Content 2..."
  }
}
```

### `/api/v1/compiler/compile/project/zip`
Compile a project from a ZIP file with `.spaceproj` structure.

**Form Data:**
- `file`: ZIP file containing project files and a `.spaceproj` file

## .spaceproj Format

The `.spaceproj` file uses links-notation to define project structure:

```
Собаки: Files/File1.doc
Кошки: Files/File2.doc
Животные: File3.doc
Животные: (Собаки Кошки)
```

This creates a graph structure:
```
Животные
├── Собаки (Files/File1.doc)
└── Кошки (Files/File2.doc)
```

## Running the Project

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run API
cd src/SpaceCompiler
dotnet run
```

The API will be available at `https://localhost:5001` with Swagger UI at `/swagger`.

## Testing

The project includes comprehensive unit tests for all services:
- TokenizerService tests
- ParserService tests
- SpaceProjParser tests

Run tests with:
```bash
dotnet test
```

## Technologies

- .NET 8
- ASP.NET Core Web API
- Swashbuckle (Swagger/OpenAPI)
- xUnit
- FluentAssertions
- Moq

## License

This project is part of the Space ecosystem.