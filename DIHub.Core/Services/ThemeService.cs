using System;
using DIHub.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DIHub.Core.Services
{
    public sealed class ThemeService : IThemeService
    {
        private readonly ILogger<ThemeService> _logger;
        private readonly ISettingsService _settings;

        public AppTheme CurrentTheme => _settings.Current.Theme;
        public AccentColor CurrentAccent => _settings.Current.Accent;

        public event EventHandler? ThemeChanged;

        public ThemeService(ISettingsService settings, ILogger<ThemeService> logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _settings.SettingsChanged += (s, e) =>
                ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetTheme(AppTheme theme)
        {
            if (CurrentTheme == theme) return;
            _settings.Update(s => s.Theme = theme);
        }

        public void SetAccent(AccentColor accent)
        {
            if (CurrentAccent == accent) return;
            _settings.Update(s => s.Accent = accent);
        }
    }
}