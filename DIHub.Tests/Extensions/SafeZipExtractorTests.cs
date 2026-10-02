using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using DIHub.Infrastructure.Extensions;
using DIHub.Tests.Extensions.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DIHub.Tests.Extensions
{
    public sealed class SafeZipExtractorTests
    {
        private static void WriteEntry(ZipArchive zip, string entryName, string content)
        {
            var entry = zip.CreateEntry(entryName);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }

        // ─────────────────────────────────────────────
        //  Happy path
        // ─────────────────────────────────────────────

        [Fact]
        public async Task Extract_NormalZip_ExtractsAllEntries()
        {
            using var temp = new TempFolder();
            var zipPath = temp.Combine("test.zip");
            var outDir = temp.Combine("out");

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                WriteEntry(zip, "manifest.json", @"{""manifest_version"":3}");
                WriteEntry(zip, "scripts/background.js", "console.log('hi');");
                WriteEntry(zip, "icons/icon.png", "fake-png");
            }

            await SafeZipExtractor.ExtractAsync(zipPath, outDir, NullLogger.Instance);

            Assert.True(File.Exists(Path.Combine(outDir, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(outDir, "scripts", "background.js")));
            Assert.True(File.Exists(Path.Combine(outDir, "icons", "icon.png")));
        }

        [Fact]
        public async Task Extract_ContentMatchesOriginal()
        {
            using var temp = new TempFolder();
            var zipPath = temp.Combine("test.zip");
            var outDir = temp.Combine("out");

            const string content = "hello world";

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                WriteEntry(zip, "file.txt", content);

            await SafeZipExtractor.ExtractAsync(zipPath, outDir, NullLogger.Instance);

            var extracted = await File.ReadAllTextAsync(Path.Combine(outDir, "file.txt"));
            Assert.Equal(content, extracted);
        }

        // ─────────────────────────────────────────────
        //  Path traversal — MUST be rejected
        // ─────────────────────────────────────────────

        [Fact]
        public async Task Extract_ParentTraversal_Rejected()
        {
            using var temp = new TempFolder();
            var zipPath = temp.Combine("evil.zip");
            var outDir = temp.Combine("out");

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                // Classic `..` traversal
                WriteEntry(zip, "../../escape.txt", "malicious");
            }

            await Assert.ThrowsAnyAsync<System.Exception>(() =>
                SafeZipExtractor.ExtractAsync(zipPath, outDir, NullLogger.Instance));
        }

        [Fact]
        public async Task Extract_AbsolutePath_Rejected()
        {
            using var temp = new TempFolder();
            var zipPath = temp.Combine("evil.zip");
            var outDir = temp.Combine("out");

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                WriteEntry(zip, "C:\\Windows\\System32\\evil.txt", "malicious");
            }

            await Assert.ThrowsAnyAsync<System.Exception>(() =>
                SafeZipExtractor.ExtractAsync(zipPath, outDir, NullLogger.Instance));
        }

        [Fact]
        public async Task Extract_BackslashTraversal_Rejected()
        {
            using var temp = new TempFolder();
            var zipPath = temp.Combine("evil.zip");
            var outDir = temp.Combine("out");

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                WriteEntry(zip, "..\\..\\escape.txt", "malicious");
            }

            await Assert.ThrowsAnyAsync<System.Exception>(() =>
                SafeZipExtractor.ExtractAsync(zipPath, outDir, NullLogger.Instance));
        }

        [Fact]
        public async Task Extract_NoEscapeOutsideTarget()
        {
            using var temp = new TempFolder();
            var zipPath = temp.Combine("evil.zip");
            var outDir = temp.Combine("out");

            var siblingPath = Path.Combine(temp.Path, "sibling.txt");

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                // Attempt to write to sibling file (outside outDir).
                WriteEntry(zip, "../sibling.txt", "should not exist");
            }

            try
            {
                await SafeZipExtractor.ExtractAsync(zipPath, outDir, NullLogger.Instance);
            }
            catch
            {
                // Expected to throw.
            }

            // Verify sibling file was NOT created.
            Assert.False(File.Exists(siblingPath));
        }

        // ─────────────────────────────────────────────
        //  Invalid input
        // ─────────────────────────────────────────────

        [Fact]
        public async Task Extract_NonexistentZip_Throws()
        {
            using var temp = new TempFolder();
            var outDir = temp.Combine("out");

            await Assert.ThrowsAnyAsync<System.Exception>(() =>
                SafeZipExtractor.ExtractAsync(
                    temp.Combine("does-not-exist.zip"),
                    outDir,
                    NullLogger.Instance));
        }

        [Fact]
        public async Task Extract_InvalidZipContent_Throws()
        {
            using var temp = new TempFolder();
            var zipPath = temp.Combine("not-a-zip.zip");
            await File.WriteAllTextAsync(zipPath, "this is not a zip");

            var outDir = temp.Combine("out");

            await Assert.ThrowsAnyAsync<System.Exception>(() =>
                SafeZipExtractor.ExtractAsync(zipPath, outDir, NullLogger.Instance));
        }
    }
}