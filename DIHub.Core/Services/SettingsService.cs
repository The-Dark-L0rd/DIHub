using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.Extensions.Logging;

namespace DIHub.Core.Services
{
    public sealed class SettingsService : ISettingsService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly ILogger<SettingsService> _logger;
        private readonly string _settingsPath;

        public AppSettings Current { get; private set; } = new();

        public event EventHandler? SettingsChanged;

        public SettingsService(ILogger<SettingsService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var folder = Path.Combine(appData, "DIHub");
            Directory.CreateDirectory(folder);
            _settingsPath = Path.Combine(folder, "settings.json");
        }

        public async Task LoadAsync(CancellationToken ct = default)
        {
            try
            {
                if (!File.Exists(_settingsPath)) return;

                await using var stream = File.OpenRead(_settingsPath);
                var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, ct);

                if (loaded is not null)
                {
                    Current = loaded;
                    SettingsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings.");
            }
        }

        public async Task SaveAsync(CancellationToken ct = default)
        {
            try
            {
                var temp = _settingsPath + ".tmp";

                await using (var stream = File.Create(temp))
                {
                    await JsonSerializer.SerializeAsync(stream, Current, JsonOptions, ct);
                }

                if (File.Exists(_settingsPath))
                    File.Replace(temp, _settingsPath, null);
                else
                    File.Move(temp, _settingsPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings.");
            }
        }

        public void Update(Action<AppSettings> mutation)
        {
            mutation(Current);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
            _ = SaveAsync();
        }
    }
}