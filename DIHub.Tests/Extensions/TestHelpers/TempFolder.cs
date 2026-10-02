using System;
using System.IO;

namespace DIHub.Tests.Extensions.TestHelpers
{
    /// <summary>
    /// Creates a temp directory and cleans it up automatically when disposed.
    /// </summary>
    internal sealed class TempFolder : IDisposable
    {
        public string Path { get; }

        public TempFolder()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "dihub_tests_" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(Path);
        }

        public string Combine(params string[] parts)
            => System.IO.Path.Combine(Path, System.IO.Path.Combine(parts));

        public string CreateSubFolder(string name)
        {
            var p = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(p);
            return p;
        }

        public string WriteFile(string relativePath, string content)
        {
            var full = System.IO.Path.Combine(Path, relativePath);
            var dir = System.IO.Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(full, content);
            return full;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, recursive: true);
            }
            catch { }
        }
    }
}