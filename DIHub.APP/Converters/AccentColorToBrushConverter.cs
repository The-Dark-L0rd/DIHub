using System;
using DIHub.Core.Interfaces;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DIHub.APP.Converters
{
    public sealed class AccentColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var color = value switch
            {
                AccentColor.Purple => Color.FromArgb(255, 0x8B, 0x5C, 0xF6),
                AccentColor.Blue   => Color.FromArgb(255, 0x3B, 0x82, 0xF6),
                AccentColor.Cyan   => Color.FromArgb(255, 0x06, 0xB6, 0xD4),
                AccentColor.Green  => Color.FromArgb(255, 0x22, 0xC5, 0x5E),
                AccentColor.Orange => Color.FromArgb(255, 0xF9, 0x73, 0x16),
                _ => Color.FromArgb(255, 0x8B, 0x5C, 0xF6)
            };

            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}