using System.Threading.Tasks;
using DIHub.Infrastructure.Extensions;
using DIHub.Tests.Extensions.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DIHub.Tests.Extensions
{
    public sealed class ExtensionValidatorTests
    {
        private static ExtensionValidator CreateValidator()
            => new(NullLogger<ExtensionValidator>.Instance);

        // ─────────────────────────────────────────────
        //  Happy path
        // ─────────────────────────────────────────────

        [Fact]
        public async Task ValidateFolder_ValidManifestV3_Succeeds()
        {
            using var temp = new TempFolder();
            temp.WriteFile("manifest.json", @"{
                ""manifest_version"": 3,
                ""name"": ""Test Extension"",
                ""version"": ""1.0.0"",
                ""description"": ""A test"",
                ""permissions"": [""storage""]
            }");

            var validator = CreateValidator();
            var result = await validator.ValidateFolderAsync(temp.Path);

            Assert.True(result.IsValid);
            Assert.NotNull(result.Manifest);
            Assert.NotNull(result.SuggestedId);
            Assert.Equal("Test Extension", result.Manifest!.Name);
            Assert.Equal("1.0.0", result.Manifest.Version);
            Assert.Equal(3, result.Manifest.ManifestVersion);
        }

        [Fact]
        public async Task ValidateFolder_ValidManifestV2_Succeeds()
        {
            using var temp = new TempFolder();
            temp.WriteFile("manifest.json", @"{
                ""manifest_version"": 2,
                ""name"": ""Legacy Extension"",
                ""version"": ""0.5.2""
            }");

            var validator = CreateValidator();
            var result = await validator.ValidateFolderAsync(temp.Path);

            Assert.True(result.IsValid);
            Assert.Equal(2, result.Manifest!.ManifestVersion);
        }

        [Fact]
        public async Task ValidateFolder_ManifestInSubfolder_DoesNotFindIt()
        {
            using var temp = new TempFolder();
            temp.WriteFile("subdir/manifest.json", @"{
                ""manifest_version"": 3,
                ""name"": ""Nested"",
                ""version"": ""1.0.0""
            }");

            var validator = CreateValidator();
            var result = await validator.ValidateFolderAsync(temp.Path);

            Assert.False(result.IsValid);
        }

        // ─────────────────────────────────────────────
        //  Failure paths
        // ─────────────────────────────────────────────

        [Fact]
        public async Task ValidateFolder_MissingManifest_Fails()
        {
            using var temp = new TempFolder();
            temp.WriteFile("readme.txt", "hello");

            var validator = CreateValidator();
            var result = await validator.ValidateFolderAsync(temp.Path);

            Assert.False(result.IsValid);
            Assert.Contains("manifest.json", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateFolder_InvalidJson_Fails()
        {
            using var temp = new TempFolder();
            temp.WriteFile("manifest.json", "{ this is not json }");

            var validator = CreateValidator();
            var result = await validator.ValidateFolderAsync(temp.Path);

            Assert.False(result.IsValid);
            Assert.Contains("JSON", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateFolder_MissingManifestVersion_Fails()
        {
            using var temp = new TempFolder();
            temp.WriteFile("manifest.json", @"{
                ""name"": ""No Version"",
                ""version"": ""1.0.0""
            }");

            var validator = CreateValidator();
            var result = await validator.ValidateFolderAsync(temp.Path);

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ValidateFolder_UnsupportedManifestVersion_Fails()
        {
            using var temp = new TempFolder();
            temp.WriteFile("manifest.json", @"{
                ""manifest_version"": 4,
                ""name"": ""Future"",
                ""version"": ""1.0.0""
            }");

            var validator = CreateValidator();
            var result = await validator.ValidateFolderAsync(temp.Path);

            Assert.False(result.IsValid);
            Assert.Contains("manifest_version", result.ErrorMessage);
        }

        [Fact]
        public async Task ValidateFolder_MissingName_Fails()
        {
            using var temp = new TempFolder();
            temp.WriteFile("manifest.json", @"{
                ""manifest_version"": 3,
                ""version"": ""1.0.0""
            }");

            var validator = CreateValidator();
            var result = await validator.ValidateFolderAsync(temp.Path);

            Assert.False(result.IsValid);
            Assert.Contains("name", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateFolder_MissingVersion_Fails()
        {
            using var temp = new TempFolder();
            temp.WriteFile("manifest.json", @"{
                ""manifest_version"": 3,
                ""name"": ""No Version""
            }");

            var validator = CreateValidator();
            var result = await validator.ValidateFolderAsync(temp.Path);

            Assert.False(result.IsValid);
            Assert.Contains("version", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ValidateFolder_InvalidVersionFormat_Fails()
        {
            using var temp = new TempFolder();
            temp.WriteFile("manifest.json", @"{
                ""manifest_version"": 3,
                ""name"": ""Bad Version"",
                ""version"": ""1.0.0-beta""
            }");

            var validator = CreateValidator();
            var result = await validator.ValidateFolderAsync(temp.Path);

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ValidateFolder_NonexistentFolder_Fails()
        {
            var validator = CreateValidator();
            var result = await validator.ValidateFolderAsync(@"C:\does\not\exist\anywhere\1234");

            Assert.False(result.IsValid);
        }

        // ─────────────────────────────────────────────
        //  Stable ID
        // ─────────────────────────────────────────────

        [Fact]
        public async Task ValidateFolder_SameContent_ProducesSameId()
        {
            using var temp1 = new TempFolder();
            using var temp2 = new TempFolder();

            const string manifest = @"{
                ""manifest_version"": 3,
                ""name"": ""Stable"",
                ""version"": ""1.0.0"",
                ""author"": ""Author""
            }";

            temp1.WriteFile("manifest.json", manifest);
            temp2.WriteFile("manifest.json", manifest);

            var validator = CreateValidator();
            var r1 = await validator.ValidateFolderAsync(temp1.Path);
            var r2 = await validator.ValidateFolderAsync(temp2.Path);

            Assert.True(r1.IsValid);
            Assert.True(r2.IsValid);
            Assert.Equal(r1.SuggestedId, r2.SuggestedId);
        }

        [Fact]
        public async Task ValidateFolder_DifferentName_ProducesDifferentId()
        {
            using var temp1 = new TempFolder();
            using var temp2 = new TempFolder();

            temp1.WriteFile("manifest.json", @"{
                ""manifest_version"": 3,
                ""name"": ""Alpha"",
                ""version"": ""1.0.0""
            }");
            temp2.WriteFile("manifest.json", @"{
                ""manifest_version"": 3,
                ""name"": ""Beta"",
                ""version"": ""1.0.0""
            }");

            var validator = CreateValidator();
            var r1 = await validator.ValidateFolderAsync(temp1.Path);
            var r2 = await validator.ValidateFolderAsync(temp2.Path);

            Assert.NotEqual(r1.SuggestedId, r2.SuggestedId);
        }
    }
}