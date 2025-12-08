namespace TheLastMageStanding.Game.Core.MetaProgression;

/// <summary>
/// Abstraction for file I/O operations to enable testability.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Checks if a file exists.
    /// </summary>
    bool FileExists(string path);

    /// <summary>
    /// Reads all text from a file.
    /// </summary>
    string ReadAllText(string path);

    /// <summary>
    /// Writes text to a file.
    /// </summary>
    void WriteAllText(string path, string content);

    /// <summary>
    /// Deletes a file.
    /// </summary>
    void DeleteFile(string path);

    /// <summary>
    /// Copies a file.
    /// </summary>
    void CopyFile(string sourcePath, string destPath, bool overwrite);

    /// <summary>
    /// Creates a directory if it doesn't exist.
    /// </summary>
    void CreateDirectory(string path);

    /// <summary>
    /// Gets files matching a pattern in a directory.
    /// </summary>
    string[] GetFiles(string directory, string searchPattern);

    /// <summary>
    /// Checks if a directory exists.
    /// </summary>
    bool DirectoryExists(string path);
}

/// <summary>
/// Default file system implementation using System.IO.
/// </summary>
public sealed class DefaultFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllText(string path, string content) => File.WriteAllText(path, content);

    public void DeleteFile(string path) => File.Delete(path);

    public void CopyFile(string sourcePath, string destPath, bool overwrite) => 
        File.Copy(sourcePath, destPath, overwrite);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public string[] GetFiles(string directory, string searchPattern) => 
        Directory.GetFiles(directory, searchPattern);

    public bool DirectoryExists(string path) => Directory.Exists(path);
}
