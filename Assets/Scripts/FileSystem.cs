using System.IO;

public static class FileSystem
{
    public static bool FolderExists(string path) => Directory.Exists(path);

    public static void CreateFolder(string path) => Directory.CreateDirectory(path);

    public static bool FileExists(string path) => File.Exists(path);

    public static void CreateFile(string path, string content) => File.WriteAllText(path, content);

    public static void EditFile(string path, string content) => File.WriteAllText(path, content);

    public static void RemoveFile(string path) => File.Delete(path);
}
