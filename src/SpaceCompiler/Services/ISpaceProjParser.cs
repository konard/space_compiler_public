using SpaceCompiler.Models;

namespace SpaceCompiler.Services
{
    /// <summary>
    /// Parser for .spaceproj files using links-notation format
    /// </summary>
    public interface ISpaceProjParser
    {
        /// <summary>
        /// Parse .spaceproj file content
        /// </summary>
        /// <param name="content">Content of the .spaceproj file</param>
        /// <returns>Project graph structure</returns>
        Task<ProjectGraph> ParseAsync(string content);
    }
}
