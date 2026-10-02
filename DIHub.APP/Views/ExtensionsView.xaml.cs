using DIHub.APP.Controls.Extensions;
using DIHub.APP.ViewModels;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;

namespace DIHub.APP.Views
{
    public sealed partial class ExtensionsView : UserControl
    {
        public ExtensionsViewModel ViewModel { get; }

        private bool _openedOnce;
        private string _activeTab = "discover";
        private CancellationTokenSource? _progressCts;

        public event EventHandler? RequestClose;

        public ExtensionsView()
        {
            InitializeComponent();
            ViewModel = App.GetService<ExtensionsViewModel>();

            ViewModel.Categories.CollectionChanged += OnCategoriesChanged;
            ViewModel.FeaturedCards.CollectionChanged += OnFeaturedChanged;
            ViewModel.AllCards.CollectionChanged += OnAllCardsChanged;
            ViewModel.InstalledItems.CollectionChanged += OnInstalledChanged;

            UpdateTabVisuals();
        }

        // ─────────────────────────────────────────────
        //  Open / Close
        //  ─────────────────────────────────────────────

        public async void Open()
        {
            Visibility = Visibility.Visible;

            if (!_openedOnce)
            {
                _openedOnce = true;
                await ViewModel.InitializeAsync();
                BuildCategoryChips();
            }
            else
            {
                await ViewModel.RefreshAsync();
            }

            Focus(FocusState.Programmatic);
        }

        /// <summary>Opens the Extensions view directly on the Diagnostics tab.</summary>
        public async void OpenDiagnostics()
        {
            Visibility = Visibility.Visible;

            if (!_openedOnce)
            {
                _openedOnce = true;
                await ViewModel.InitializeAsync();
                BuildCategoryChips();
            }
            else
            {
                await ViewModel.RefreshAsync();
            }

            SwitchTab("diagnostics");
            Focus(FocusState.Programmatic);
        }

        public void Close()
        {
            _progressCts?.Cancel();
            Visibility = Visibility.Collapsed;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        // ─────────────────────────────────────────────
        //  Keyboard
        //  ─────────────────────────────────────────────

        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == VirtualKey.Escape)
            {
                e.Handled = true;
                Close();
            }
        }

        // ─────────────────────────────────────────────
        //  Tabs
        //  ─────────────────────────────────────────────

        private void OnTabClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string tag) return;
            SwitchTab(tag);
        }

        private void SwitchTab(string tag)
        {
            _activeTab = tag;

            ContentDiscover.Visibility = tag == "discover" ? Visibility.Visible : Visibility.Collapsed;
            ContentInstalled.Visibility = tag == "installed" ? Visibility.Visible : Visibility.Collapsed;
            ContentUpdates.Visibility = tag == "updates" ? Visibility.Visible : Visibility.Collapsed;
            ContentDiagnostics.Visibility = tag == "diagnostics" ? Visibility.Visible : Visibility.Collapsed;

            // Refresh diagnostics when its tab is shown.
            if (tag == "diagnostics")
                ContentDiagnostics.Refresh();

            UpdateTabVisuals();
        }

        private void UpdateTabVisuals()
        {
            var accentBrush = (Brush?)Application.Current.Resources["AppAccentBrush"]
                              ?? new SolidColorBrush(Colors.Purple);
            var transparent = new SolidColorBrush(Colors.Transparent);
            var primaryBrush = (Brush?)Application.Current.Resources["AppTextPrimaryBrush"]
                               ?? new SolidColorBrush(Colors.White);
            var secondaryBrush = (Brush?)Application.Current.Resources["AppTextSecondaryBrush"]
                                 ?? new SolidColorBrush(Colors.Gray);

            StyleTab(TabDiscoverButton, _activeTab == "discover", accentBrush, transparent, primaryBrush, secondaryBrush);
            StyleTab(TabInstalledButton, _activeTab == "installed", accentBrush, transparent, primaryBrush, secondaryBrush);
            StyleTab(TabUpdatesButton, _activeTab == "updates", accentBrush, transparent, primaryBrush, secondaryBrush);
            StyleTab(TabDiagnosticsButton, _activeTab == "diagnostics", accentBrush, transparent, primaryBrush, secondaryBrush);
        }

        private static void StyleTab(
            Button btn, bool isActive,
            Brush accent, Brush transparent,
            Brush primary, Brush secondary)
        {
            btn.Background = isActive
                ? new SolidColorBrush(Color.FromArgb(30, 0x8B, 0x5C, 0xF6))
                : transparent;

            btn.BorderThickness = new Thickness(0);
            btn.CornerRadius = new CornerRadius(6);
            btn.Foreground = isActive ? primary : secondary;
            btn.FontWeight = isActive
                ? Microsoft.UI.Text.FontWeights.SemiBold
                : Microsoft.UI.Text.FontWeights.Normal;
        }

        // ─────────────────────────────────────────────
        //  Category chips
        //  ─────────────────────────────────────────────

        private void OnCategoriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => BuildCategoryChips();

        private void BuildCategoryChips()
        {
            CategoryChipsPanel.Children.Clear();

            foreach (var category in ViewModel.Categories)
            {
                var captured = category;
                var btn = new Button
                {
                    Content = category,
                    Tag = category,
                    FontSize = 11,
                    Padding = new Thickness(10, 4, 10, 4),
                    CornerRadius = new CornerRadius(6)
                };

                var isSelected = string.Equals(
                    ViewModel.SelectedCategory, category,
                    StringComparison.OrdinalIgnoreCase);

                ApplyChipStyle(btn, isSelected);

                btn.Click += (s, ev) =>
                {
                    ViewModel.SelectedCategory = captured;
                    foreach (var child in CategoryChipsPanel.Children)
                        if (child is Button b)
                            ApplyChipStyle(b, string.Equals(b.Tag as string, captured, StringComparison.OrdinalIgnoreCase));
                };

                CategoryChipsPanel.Children.Add(btn);
            }
        }

        private static void ApplyChipStyle(Button btn, bool selected)
        {
            var accent = (Brush?)Application.Current.Resources["AppAccentBrush"]
                         ?? new SolidColorBrush(Colors.Purple);
            var border = (Brush?)Application.Current.Resources["AppBorderBrush"]
                         ?? new SolidColorBrush(Colors.DarkGray);
            var secondary = (Brush?)Application.Current.Resources["AppTextSecondaryBrush"]
                            ?? new SolidColorBrush(Colors.Gray);

            if (selected)
            {
                btn.Background = new SolidColorBrush(Color.FromArgb(30, 0x8B, 0x5C, 0xF6));
                btn.BorderBrush = accent;
                btn.Foreground = accent;
            }
            else
            {
                btn.Background = (Brush)Application.Current.Resources["AppSecondaryBrush"];
                btn.BorderBrush = border;
                btn.Foreground = secondary;
            }

            btn.BorderThickness = new Thickness(1);
        }

        // ─────────────────────────────────────────────
        //  Empty state sync
        //  ─────────────────────────────────────────────

        private void OnFeaturedChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (FeaturedSection is null) return;
            FeaturedSection.Visibility = ViewModel.FeaturedCards.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void OnAllCardsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (DiscoverEmptyState is null) return;
            DiscoverEmptyState.Visibility = ViewModel.AllCards.Count > 0
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void OnInstalledChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (InstalledEmptyState is null) return;
            InstalledEmptyState.Visibility = ViewModel.InstalledItems.Count > 0
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        // ─────────────────────────────────────────────
        //  Install from local folder / ZIP
        //  ─────────────────────────────────────────────

        private async void OnInstallFromFolderClicked(object sender, RoutedEventArgs e)
        {
            var hwnd = GetWindowHandle();
            if (hwnd == IntPtr.Zero) return;

            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.FileTypeFilter.Add("*");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder is null) return;

            var result = await ViewModel.InstallFromFolderAsync(folder.Path);
            await ShowInstallResultAsync(result);
        }

        private async void OnInstallFromZipClicked(object sender, RoutedEventArgs e)
        {
            var hwnd = GetWindowHandle();
            if (hwnd == IntPtr.Zero) return;

            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.Downloads;
            picker.FileTypeFilter.Add(".zip");
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file is null) return;

            var result = await ViewModel.InstallFromZipAsync(file.Path);
            await ShowInstallResultAsync(result);
        }

        // ─────────────────────────────────────────────
        //  Install from URL
        //  ─────────────────────────────────────────────

        private async void OnInstallFromUrlClicked(object sender, RoutedEventArgs e)
            => await ShowInstallFromUrlDialogAsync();

        private async Task ShowInstallFromUrlDialogAsync()
        {
            var urlBox = new TextBox
            {
                PlaceholderText = "https://example.com/extension.zip",
                FontSize = 12,
                Padding = new Thickness(10, 8, 10, 8)
            };

            var shaBox = new TextBox
            {
                PlaceholderText = "(optional)",
                FontSize = 11,
                Padding = new Thickness(10, 8, 10, 8)
            };

            var warningText = new TextBlock
            {
                Text = "⚠ Only install extensions from sources you trust. " +
                       "DI Hub cannot verify the contents of a remote ZIP.",
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 0xF9, 0x73, 0x16))
            };

            var panel = new StackPanel { Spacing = 10, Width = 440 };

            panel.Children.Add(new TextBlock
            {
                Text = "Paste the direct download URL of an unpacked extension ZIP.",
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            });

            panel.Children.Add(urlBox);

            panel.Children.Add(new TextBlock
            {
                Text = "SHA-256 checksum (optional)",
                FontSize = 11,
                Margin = new Thickness(0, 6, 0, 0)
            });

            panel.Children.Add(shaBox);
            panel.Children.Add(warningText);

            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Install from URL",
                Content = panel,
                PrimaryButtonText = "Download & Install",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var url = urlBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(url)) return;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                await ShowMessageAsync("Invalid URL", "Only https:// URLs are supported.");
                return;
            }

            var sha = shaBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(sha)) sha = null;

            await RunUrlInstallAsync(url, sha);
        }

        private async Task RunUrlInstallAsync(string url, string? sha256)
        {
            _progressCts?.Dispose();
            _progressCts = new CancellationTokenSource();
            var ct = _progressCts.Token;

            ProgressOverlay.Visibility = Visibility.Visible;
            ProgressTitle.Text = "Installing extension...";
            ProgressSubtitle.Text = new Uri(url).Host;
            ProgressDetail.Text = "Starting download...";
            ProgressBar.Value = 0;
            ProgressBar.IsIndeterminate = true;
            ProgressCancelButton.IsEnabled = true;

            var progress = new Progress<DownloadProgress>(p =>
            {
                ProgressDetail.Text = p.CurrentStage switch
                {
                    "Downloading" => FormatDownloadProgress(p),
                    "Verifying" => "Verifying SHA-256 checksum...",
                    "Installing" => "Extracting and installing...",
                    _ => p.CurrentStage ?? string.Empty
                };

                if (p.CurrentStage == "Downloading" && p.TotalBytes > 0)
                {
                    ProgressBar.IsIndeterminate = false;
                    ProgressBar.Value = p.PercentComplete * 100.0;
                }
                else
                {
                    ProgressBar.IsIndeterminate = true;
                }
            });

            ExtensionInstallResult result;
            try
            {
                result = await ViewModel.InstallFromUrlAsync(url, sha256, progress, ct);
            }
            catch (OperationCanceledException)
            {
                result = ExtensionInstallResult.Fail("Download was cancelled.");
            }
            catch (Exception ex)
            {
                result = ExtensionInstallResult.Fail(ex.Message);
            }
            finally
            {
                ProgressOverlay.Visibility = Visibility.Collapsed;
                _progressCts?.Dispose();
                _progressCts = null;
            }

            await ShowInstallResultAsync(result);
        }

        private static string FormatDownloadProgress(DownloadProgress p)
        {
            var receivedMb = p.BytesReceived / (1024.0 * 1024.0);

            if (p.TotalBytes > 0)
            {
                var totalMb = p.TotalBytes / (1024.0 * 1024.0);
                return $"Downloading... {receivedMb:F1} MB / {totalMb:F1} MB " +
                       $"({p.PercentComplete * 100:F0}%)";
            }

            return $"Downloading... {receivedMb:F1} MB";
        }

        private void OnProgressCancelClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                ProgressCancelButton.IsEnabled = false;
                ProgressDetail.Text = "Cancelling...";
                _progressCts?.Cancel();
            }
            catch { }
        }

        private void OnProgressBackdropTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        // ─────────────────────────────────────────────
        //  Card actions
        //  ─────────────────────────────────────────────

        private async void OnCardActionRequested(object? sender, ExtensionActionEventArgs e)
        {
            switch (e.Kind)
            {
                case ExtensionActionKind.Install:
                    await HandleInstallFromCardAsync(e.CatalogItem);
                    break;

                case ExtensionActionKind.Details:
                    await ShowDetailsAsync(e.CatalogItem);
                    break;
            }
        }

        private async Task HandleInstallFromCardAsync(ExtensionCatalogItem? item)
        {
            if (item is null) return;

            if (!string.IsNullOrWhiteSpace(item.DownloadUrl))
            {
                await HandleOneClickInstallAsync(item);
                return;
            }

            await HandleManualInstallHintAsync(item);
        }

        private async Task HandleOneClickInstallAsync(ExtensionCatalogItem item)
        {
            var url = item.DownloadUrl!;
            Uri uri;
            try { uri = new Uri(url); }
            catch { await ShowMessageAsync("Invalid URL", "The catalog entry has an invalid URL."); return; }

            var panel = new StackPanel { Spacing = 10, Width = 440 };

            panel.Children.Add(new TextBlock
            {
                Text = item.Name,
                FontSize = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Version {item.Version}  ·  {item.Author}",
                FontSize = 11,
                Opacity = 0.7
            });

            panel.Children.Add(new TextBlock
            {
                Text = item.Description ?? string.Empty,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"Source: {uri.Host}",
                FontSize = 11,
                Margin = new Thickness(0, 8, 0, 0),
                Foreground = (Brush)Application.Current.Resources["AppTextSecondaryBrush"]
            });

            if (item.DeclaredPermissions.Length > 0)
            {
                panel.Children.Add(new ExtensionPermissionList
                {
                    Permissions = item.DeclaredPermissions,
                    HostPermissions = Array.Empty<string>()
                });
            }

            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Confirm installation",
                Content = new ScrollViewer
                {
                    MaxHeight = 500,
                    Content = panel
                },
                PrimaryButtonText = "Download & Install",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            await RunUrlInstallAsync(url, item.Sha256);
        }

        private async Task HandleManualInstallHintAsync(ExtensionCatalogItem item)
        {
            var message =
                $"{item.Name} is listed in the DI Hub catalog, but no automatic " +
                "download URL is available for it.\n\n" +
                "To install it, please download the extension package from its " +
                "official source and use \"Install → From URL…\", \"From ZIP archive…\", " +
                "or \"From folder…\".\n\n" +
                "Official source:\n" + (item.HomepageUrl ?? "(not specified)");

            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = $"Install {item.Name}",
                Content = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12
                },
                PrimaryButtonText = "Open source page",
                SecondaryButtonText = "Install from URL…",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var url = item.HomepageUrl;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    try
                    {
                        var browser = App.GetService<IBrowserService>();
                        browser.OpenInDefaultBrowser(url);
                    }
                    catch { }
                }
            }
            else if (result == ContentDialogResult.Secondary)
            {
                await ShowInstallFromUrlDialogAsync();
            }
        }

        private async Task ShowDetailsAsync(ExtensionCatalogItem? item)
        {
            if (item is null) return;

            var panel = new StackPanel { Spacing = 10, Width = 440 };

            panel.Children.Add(new TextBlock
            {
                Text = item.Name,
                FontSize = 15,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            });

            if (!string.IsNullOrWhiteSpace(item.Version))
                panel.Children.Add(new TextBlock
                {
                    Text = $"Version {item.Version}",
                    FontSize = 11,
                    Opacity = 0.7
                });

            if (!string.IsNullOrWhiteSpace(item.Author))
                panel.Children.Add(new TextBlock
                {
                    Text = $"By {item.Author}",
                    FontSize = 11,
                    Opacity = 0.7
                });

            panel.Children.Add(new TextBlock
            {
                Text = item.Description ?? string.Empty,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 6, 0, 6)
            });

            if (item.DeclaredPermissions.Length > 0)
            {
                panel.Children.Add(new ExtensionPermissionList
                {
                    Permissions = item.DeclaredPermissions,
                    HostPermissions = Array.Empty<string>()
                });
            }

            panel.Children.Add(new TextBlock
            {
                Text = item.DownloadUrl is null
                    ? "Automatic install: not available for this entry"
                    : "Automatic install: available",
                FontSize = 10,
                Opacity = 0.6,
                Margin = new Thickness(0, 8, 0, 0)
            });

            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = "Extension details",
                Content = new ScrollViewer
                {
                    MaxHeight = 480,
                    Content = panel
                },
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }

        // ─────────────────────────────────────────────
        //  Installed item actions
        //  ─────────────────────────────────────────────

        private void OnItemActionRequested(object? sender, ExtensionActionEventArgs e)
        {
            var ext = e.InstalledExtension;
            if (ext is null) return;

            switch (e.Kind)
            {
                case ExtensionActionKind.Enable:
                case ExtensionActionKind.Disable:
                    _ = HandleToggleAsync(ext, e.Kind == ExtensionActionKind.Enable);
                    break;

                case ExtensionActionKind.Remove:
                    _ = ConfirmAndRemoveAsync(ext);
                    break;

                case ExtensionActionKind.ManageScope:
                    _ = ShowManageScopeAsync(ext);
                    break;

                case ExtensionActionKind.Details:
                    _ = ShowInstalledDetailsAsync(ext);
                    break;
            }
        }

        /// <summary>
        /// If the effective state comes from a non-Global scope, opening
        /// Manage Scope is the correct action (a Global toggle would have
        /// no visible effect).
        /// </summary>
        private async Task HandleToggleAsync(ExtensionInfo ext, bool enable)
        {
            var item = ViewModel.InstalledItems
                .FirstOrDefault(i => i.Info.Id == ext.Id);

            if (item is not null &&
                (item.State.Source == ExtensionScope.Account ||
                 item.State.Source == ExtensionScope.Service))
            {
                await ShowManageScopeAsync(ext);
                return;
            }

            await ViewModel.SetGlobalEnabledAsync(ext.Id, enable);
        }

        private async Task ShowManageScopeAsync(ExtensionInfo ext)
        {
            var panel = new ManageScopeDialog();
            panel.Load(ext);

            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = $"Manage scope — {ext.Name}",
                Content = panel,
                PrimaryButtonText = "Apply",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var selection = panel.GetSelection();
            if (selection is null)
            {
                await ShowMessageAsync(
                    "Invalid selection",
                    "Please select a valid scope and target before applying.");
                return;
            }

            await ViewModel.SetScopeAsync(
                ext.Id,
                selection.Scope,
                selection.ServiceId,
                selection.AccountId,
                selection.Enabled);

            await ShowMessageAsync(
                "Scope updated",
                $"Assignment saved at the {DescribeScope(selection.Scope)} scope.");
        }

        private static string DescribeScope(ExtensionScope scope) => scope switch
        {
            ExtensionScope.Global => "Global",
            ExtensionScope.Service => "Service",
            ExtensionScope.Account => "Account",
            _ => "Unknown"
        };

        private async Task ConfirmAndRemoveAsync(ExtensionInfo ext)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = $"Remove {ext.Name}?",
                Content = new TextBlock
                {
                    Text =
                        "The extension will be disabled and uninstalled from all DI Hub " +
                        "profiles. Data stored by the extension inside the browser profile " +
                        "is not removed by this action.",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12
                },
                PrimaryButtonText = "Remove",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            await ViewModel.RemoveExtensionAsync(ext.Id);
        }

        private async Task ShowInstalledDetailsAsync(ExtensionInfo ext)
        {
            await ShowMessageAsync(
                ext.Name,
                $"Version: {ext.Version}\n" +
                $"Author: {ext.Author ?? "(unknown)"}\n" +
                $"Manifest: v{ext.ManifestVersion}\n" +
                $"Source: {ext.Source}\n" +
                (ext.SourceUrl is not null ? $"URL: {ext.SourceUrl}\n" : "") +
                $"Path: {ext.InstallPath}");
        }

        // ─────────────────────────────────────────────
        //  Shared helpers
        //  ─────────────────────────────────────────────

        private async Task ShowInstallResultAsync(ExtensionInstallResult result)
        {
            if (result.Success && result.Extension is not null)
            {
                await ShowMessageAsync(
                    "Extension installed",
                    $"{result.Extension.Name} v{result.Extension.Version} has been installed.\n\n" +
                    "Open or reload a tab to activate the extension.");
            }
            else
            {
                await ShowMessageAsync(
                    "Installation failed",
                    result.ErrorMessage ?? "Unknown error.");
            }
        }

        private async Task ShowMessageAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = XamlRoot,
                Title = title,
                Content = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12
                },
                CloseButtonText = "OK",
                DefaultButton = ContentDialogButton.Close
            };

            await dialog.ShowAsync();
        }

        private static IntPtr GetWindowHandle()
        {
            try
            {
                var win = App.GetMainWindow();
                if (win is null) return IntPtr.Zero;
                return WinRT.Interop.WindowNative.GetWindowHandle(win);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        // ─────────────────────────────────────────────
        //  Backdrop / panel
        //  ─────────────────────────────────────────────

        private void OnBackdropTapped(object sender, TappedRoutedEventArgs e)
            => Close();

        private void OnPanelTapped(object sender, TappedRoutedEventArgs e)
            => e.Handled = true;

        private void OnCloseClicked(object sender, RoutedEventArgs e) => Close();
    }
}