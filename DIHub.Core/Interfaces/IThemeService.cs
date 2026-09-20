using System;

namespace DIHub.Core.Interfaces
{
    public interface IThemeService
    {
        AppTheme CurrentTheme { get; }
        AccentColor CurrentAccent { get; }

        void SetTheme(AppTheme theme);
        void SetAccent(AccentColor accent);

        event EventHandler? ThemeChanged;
    }

    public enum AppTheme { Dark, Light, System }
    public enum AccentColor { Purple, Blue, Cyan, Green, Orange }
}