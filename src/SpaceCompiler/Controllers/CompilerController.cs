using Microsoft.AspNetCore.Mvc;
using SpaceCompiler.Models;
using SpaceCompiler.Services;
using System.Text;

namespace SpaceCompiler.Controllers
{
    /// <summary>
    /// Compiler API Controller for compiling documents into structured AST with semantic analysis
    /// </summary>
    [ApiController]
    [Route("api/v1/compiler")]
    [Produces("application/json")]
    public class CompilerController : ControllerBase
    {
        private readonly ICompilationService _compilationService;
        private readonly ILogger<CompilerController> _logger;

        public CompilerController(
            ICompilationService compilationService,
            ILogger<CompilerController> logger)
        {
            _compilationService = compilationService ?? throw new ArgumentNullException(nameof(compilationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Compile a single file
        /// </summary>
        /// <param name="request">File compilation request</param>
        /// <returns>Compilation result with AST and semantic analysis</returns>
        [HttpPost("compile/file")]
        [ProducesResponseType(typeof(CompilationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CompilationResult>> CompileFile([FromBody] CompileFileRequest request)
        {
            _logger.LogInformation("Compiling single file: {FileName}", request.FileName);

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { error = "Content is required" });
            }

            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                return BadRequest(new { error = "FileName is required" });
            }

            try
            {
                var result = await _compilationService.CompileFileAsync(
                    request.Content,
                    request.FileName,
                    request.ContentType);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compiling file {FileName}", request.FileName);
                return StatusCode(500, new { error = $"Error compiling file: {ex.Message}" });
            }
        }

        /// <summary>
        /// Compile multiple files as a unified tree
        /// </summary>
        /// <param name="request">Multiple files compilation request</param>
        /// <returns>Compilation result with unified AST</returns>
        [HttpPost("compile/files")]
        [ProducesResponseType(typeof(CompilationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CompilationResult>> CompileFiles([FromBody] CompileFilesRequest request)
        {
            _logger.LogInformation("Compiling multiple files: {Count}", request.Files?.Count ?? 0);

            if (request.Files == null || request.Files.Count == 0)
            {
                return BadRequest(new { error = "At least one file is required" });
            }

            try
            {
                var result = await _compilationService.CompileFilesAsync(request.Files);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compiling multiple files");
                return StatusCode(500, new { error = $"Error compiling files: {ex.Message}" });
            }
        }

        /// <summary>
        /// Compile a project from a zip file with .spaceproj structure
        /// </summary>
        /// <param name="file">Zip file containing project files and .spaceproj file</param>
        /// <returns>Compilation result with project graph</returns>
        [HttpPost("compile/project/zip")]
        [ProducesResponseType(typeof(CompilationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<CompilationResult>> CompileProject(IFormFile file)
        {
            _logger.LogInformation("Compiling project from zip: {FileName}", file?.FileName);

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Zip file is required" });
            }

            if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "File must be a .zip archive" });
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _compilationService.CompileProjectAsync(stream);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compiling project from zip");
                return StatusCode(500, new { error = $"Error compiling project: {ex.Message}" });
            }
        }
    }

    /// <summary>
    /// Request model for compiling a single file
    /// </summary>
    public class CompileFileRequest
    {
        /// <summary>
        /// File content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// File name
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Content type (optional, auto-detected if not provided)
        /// </summary>
        public string? ContentType { get; set; }
    }

    /// <summary>
    /// Request model for compiling multiple files
    /// </summary>
    public class CompileFilesRequest
    {
        /// <summary>
        /// Dictionary of file names and their contents
        /// </summary>
        public Dictionary<string, string> Files { get; set; } = new();
    }
}
