using SpaceCompiler.Models;

namespace SpaceCompiler.Services
{
    /// <summary>
    /// Main compilation service orchestrating tokenization, parsing, and analysis
    /// </summary>
    public interface ICompilationService
    {
        /// <summary>
        /// Compile a single file
        /// </summary>
        /// <param name="content">File content</param>
        /// <param name="fileName">File name</param>
        /// <param name="contentType">Content type (auto-detected if not specified)</param>
        /// <returns>Compilation result</returns>
        Task<CompilationResult> CompileFileAsync(string content, string fileName, string? contentType = null);

        /// <summary>
        /// Compile multiple files as a unified tree
        /// </summary>
        /// <param name="files">Dictionary of file names and contents</param>
        /// <returns>Compilation result with unified AST</returns>
        Task<CompilationResult> CompileFilesAsync(Dictionary<string, string> files);

        /// <summary>
        /// Compile a project from zip file with .spaceproj structure
        /// </summary>
        /// <param name="zipStream">Zip file stream</param>
        /// <returns>Compilation result with project graph</returns>
        Task<CompilationResult> CompileProjectAsync(Stream zipStream);
    }
}
