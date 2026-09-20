using System;
using System.Collections.Generic;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DIHub.APP.Views
{
    public sealed partial class PanelTargetPickerDialog : UserControl
    {
        private readonly IAIServiceManager _serviceManager;

        public event EventHandler<AIAccount>? AccountSelected;

        public PanelTargetPickerDialog()
        {
            InitializeComponent();
            _serviceManager = App.GetService<IAIServiceManager>();
            BuildList();
        }

        private void BuildList()
        {
            ServicesStack.Children.Clear();

            var accentBrush = Application.Current.Resources["AppAccentBrush"] as Brush
                              ?? new SolidColorBrush(Colors.Purple);

            foreach (var svc in _serviceManager.Services)
            {
                // Service header
                var headerRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Margin = new Thickness(4, 12, 0, 4)
                };

                headerRow.Children.Add(new FontIcon
                {
                    Glyph = svc.Icon,
                    FontSize = 13,
                    Foreground = accentBrush,
                    VerticalAlignment = VerticalAlignment.Center
                });

                headerRow.Children.Add(new TextBlock
                {
                    Text = svc.Name,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                });

                ServicesStack.Children.Add(headerRow);

                // Accounts
                foreach (var acc in svc.Accounts)
                {
                    ServicesStack.Children.Add(BuildAccountButton(svc, acc));
                }
            }
        }

        private Button BuildAccountButton(AIService svc, AIAccount acc)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            row.Children.Add(new FontIcon
            {
                Glyph = acc.Icon,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            });

            row.Children.Add(new TextBlock
            {
                Text = acc.Name,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            });

            if (acc.IsDefault)
            {
                row.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 0x8B, 0x5C, 0xF6)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 1, 6, 1),
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = new TextBlock
                    {
                        Text = "default",
                        FontSize = 9,
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 0x8B, 0x5C, 0xF6))
                    }
                });
            }

            var button = new Button
            {
                Content = row,
                Tag = acc,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,   // ← fix
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 1, 0, 1),
                Background = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(6)
            };

            button.Click += (s, e) =>
            {
                if (s is Button b && b.Tag is AIAccount a)
                    AccountSelected?.Invoke(this, a);
            };

            return button;
        }
    }
}