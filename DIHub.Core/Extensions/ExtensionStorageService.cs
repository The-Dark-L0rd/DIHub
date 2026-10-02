using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace DIHub.Infrastructure.Extensions
{
    public sealed class ExtensionStorageService : IExtensionStorageService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ILogger<ExtensionStorageService> _logger;
        private readonly string _extensionsConfigPath;
        private readonly string _packageRoot;
        private readonly string _tempRoot;

        public ExtensionStorageService(ILogger<ExtensionStorageService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var configDir = Path.Combine(roaming, "DIHub");
            Directory.CreateDirectory(configDir);
            _extensionsConfigPath = Path.Combine(configDir, "extensions.json");

            _packageRoot = Path.Combine(local, "DIHub", "Extensions");
            _tempRoot = Path.Combine(local, "DIHub", "Temp", "Extensions");
        }

        public string GetPackageRootPath()
        {
            Directory.CreateDirectory(_packageRoot);
            return _packageRoot;
        }

        public string GetTempRootPath()
        {
            Directory.CreateDirectory(_tempRoot);
            return _tempRoot;
        }

        public async Task<ExtensionConfigFile> LoadAsync(CancellationToken ct = default)
        {
            if (!File.Exists(_extensionsConfigPath))
                return new ExtensionConfigFile();

            try
            {
                await using var stream = File.OpenRead(_extensionsConfigPath);
                var config = await JsonSerializer.DeserializeAsync<ExtensionConfigFile>(
                    stream, JsonOptions, ct);
                return config ?? new ExtensionConfigFile();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read extensions.json. Returning empty config.");
                return new ExtensionConfigFile();
            }
        }

        public async Task SaveAsync(ExtensionConfigFile config, CancellationToken ct = default)
        {
            try
            {
                var temp = _extensionsConfigPath + ".tmp";

                await using (var stream = File.Create(temp))
                {
                    await JsonSerializer.SerializeAsync(stream, config, JsonOptions, ct);
                }

                if (File.Exists(_extensionsConfigPath))
                    File.Replace(temp, _extensionsConfigPath, destinationBackupFileName: null);
                else
                    File.Move(temp, _extensionsConfigPath);

                _logger.LogDebug("Extensions configuration saved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save extensions.json.");
            }
        }
    }
}