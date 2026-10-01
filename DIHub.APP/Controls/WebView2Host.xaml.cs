using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DIHub.Core.Interfaces;
using DIHub.Core.Security;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace DIHub.APP.Controls
{
    public sealed class NavigationStateChangedEventArgs : EventArgs
    {
        public bool CanGoBack { get; init; }
        public bool CanGoForward { get; init; }
        public bool IsLoading { get; init; }
        public string Url { get; init; } = string.Empty;
    }

    public sealed partial class WebView2Host : UserControl
    {
        private CoreWebView2Environment? _environment;
        private bool _isInitialized;
        private bool _disposed;
        private string? _pendingUrl;
        private CancellationTokenSource? _navCts;
        private string? _profilePath;

        public event EventHandler<string>? NavigationCompleted;
        public event EventHandler<string>? TitleChanged;
        public event EventHandler<NavigationStateChangedEventArgs>? NavigationStateChanged;

        public string? CurrentUrl => WebView.CoreWebView2?.Source;
        public bool CanGoBack => WebView.CoreWebView2?.CanGoBack == true;
        public bool CanGoForward => WebView.CoreWebView2?.CanGoForward == true;
        public CoreWebView2? Core => WebView.CoreWebView2;

        public WebView2Host()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public void SetProfilePath(string path)
        {
            if (_isInitialized)
                throw new InvalidOperationException("Cannot set profile path after initialization.");
            _profilePath = path;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_disposed) return;
            if (_isInitialized) return;

            await InitializeWebViewAsync();

            if (!string.IsNullOrEmpty(_pendingUrl))
            {
                var url = _pendingUrl;
                _pendingUrl = null;
                Navigate(url);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _navCts?.Cancel();
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                var userDataFolder = !string.IsNullOrWhiteSpace(_profilePath)
                    ? _profilePath!
                    : Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "DIHub", "WebView2");

                Directory.CreateDirectory(userDataFolder);

                _environment = await CoreWebView2Environment.CreateWithOptionsAsync(
                    browserExecutableFolder: null,
                    userDataFolder: userDataFolder,
                    options: new CoreWebView2EnvironmentOptions
                    {
                        Language = "en-US",
                        AdditionalBrowserArguments = "--disable-features=msWebOOUI,msPdfOOUI,msSmartScreenProtection"
                    });

                await WebView.EnsureCoreWebView2Async(_environment);

                // Runtime best-effort: enable external (Explorer) drop on the
                // WebView2 surface. The property may not exist in all WinAppSDK
                // versions, so we use reflection.
                TryEnableExternalDrop();

                ConfigureWebView();
                WireEvents();

                _isInitialized = true;
                RaiseStateChanged(isLoading: false);
            }
            catch
            {
                ShowError("WebView2 initialization failed.",
                    "Unable to start the embedded browser.");
            }
        }

        /// <summary>
        /// Best-effort reflection: set AllowExternalDrop=true so files can be
        /// dropped directly onto the panel from Explorer. Silently no-ops when
        /// the property isn't available in the current SDK.
        /// </summary>
        private void TryEnableExternalDrop()
        {
            // Try on the WinUI 3 wrapper control.
            try
            {
                var prop = WebView.GetType().GetProperty(
                    "AllowExternalDrop",
                    BindingFlags.Public | BindingFlags.Instance);

                if (prop is not null && prop.CanWrite && prop.PropertyType == typeof(bool))
                    prop.SetValue(WebView, true);
            }
            catch { /* best-effort */ }

            // Some SDK versions expose it on the CoreWebView2 instance instead.
            try
            {
                if (WebView.CoreWebView2 is not null)
                {
                    var coreProp = WebView.CoreWebView2.GetType().GetProperty(
                        "AllowExternalDrop",
                        BindingFlags.Public | BindingFlags.Instance);

                    if (coreProp is not null && coreProp.CanWrite && coreProp.PropertyType == typeof(bool))
                        coreProp.SetValue(WebView.CoreWebView2, true);
                }
            }
            catch { /* best-effort */ }
        }

        private void ConfigureWebView()
        {
            if (WebView.CoreWebView2 is null) return;

            var settings = WebView.CoreWebView2.Settings;
            settings.IsScriptEnabled = true;
            settings.IsWebMessageEnabled = true;
            settings.AreDefaultContextMenusEnabled = true;
            settings.AreDevToolsEnabled = true;
            settings.IsStatusBarEnabled = false;
            settings.IsZoomControlEnabled = true;
            settings.AreBrowserAcceleratorKeysEnabled = true;
        }

        private void WireEvents()
        {
            if (WebView.CoreWebView2 is null) return;

            WebView.CoreWebView2.NavigationStarting += OnNavigationStarting;
            WebView.CoreWebView2.NavigationCompleted += OnNavigationCompletedInternal;
            WebView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
            WebView.CoreWebView2.DocumentTitleChanged += OnDocumentTitleChanged;
            WebView.CoreWebView2.ProcessFailed += OnProcessFailed;
        }

        public async void Navigate(string url)
        {
            if (_disposed) return;
            if (string.IsNullOrWhiteSpace(url)) return;

            if (!UrlPolicy.IsWebUrl(url))
            {
                ShowError("Cannot load this link.",
                    "Only http:// and https:// links are allowed.");
                return;
            }

            if (!_isInitialized)
            {
                _pendingUrl = url;
                return;
            }

            _navCts?.Cancel();
            _navCts = new CancellationTokenSource();
            var token = _navCts.Token;

            try
            {
                HideError();
                WebView.CoreWebView2?.Stop();

                var currentSource = WebView.CoreWebView2?.Source;
                if (!string.IsNullOrEmpty(currentSource) &&
                    !currentSource.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
                {
                    var tcs = new TaskCompletionSource<bool>();

                    void OnBlankCompleted(object? s, CoreWebView2NavigationCompletedEventArgs e)
                    {
                        if (WebView.CoreWebView2 != null)
                            WebView.CoreWebView2.NavigationCompleted -= OnBlankCompleted;
                        tcs.TrySetResult(true);
                    }

                    WebView.CoreWebView2!.NavigationCompleted += OnBlankCompleted;
                    WebView.CoreWebView2.Navigate("about:blank");

                    await Task.WhenAny(tcs.Task, Task.Delay(400, token));

                    if (token.IsCancellationRequested || _disposed) return;
                }

                await Task.Delay(80, token);
                if (token.IsCancellationRequested || _disposed) return;

                WebView.CoreWebView2?.Navigate(url);
            }
            catch (OperationCanceledException) { }
            catch
            {
                if (!_disposed)
                    ShowError("Unable to navigate.", "Please try again.");
            }
        }

        public void GoBack()
        {
            if (_disposed) return;
            if (WebView.CoreWebView2?.CanGoBack == true) WebView.CoreWebView2.GoBack();
        }

        public void GoForward()
        {
            if (_disposed) return;
            if (WebView.CoreWebView2?.CanGoForward == true) WebView.CoreWebView2.GoForward();
        }

        public void Reload()
        {
            if (_disposed) return;
            WebView.CoreWebView2?.Reload();
        }

        public void Stop()
        {
            if (_disposed) return;
            WebView.CoreWebView2?.Stop();
        }

        public void OpenDevTools()
        {
            if (_disposed) return;
            try { WebView.CoreWebView2?.OpenDevToolsWindow(); } catch { }
        }

        public async Task<string> ExecuteScriptAsync(string script)
        {
            if (_disposed || WebView.CoreWebView2 is null) return "null";
            try { return await WebView.CoreWebView2.ExecuteScriptAsync(script); }
            catch { return "null"; }
        }

        // ─────────────────────────────────────────────
        //  File attachment via CDP (used by Multi-AI composer)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Injects a list of local file paths into the first &lt;input type="file"&gt;
        /// element on the current page, using Chrome DevTools Protocol.
        /// Returns true if injection succeeded.
        /// </summary>
        public async Task<bool> TryAttachFilesAsync(IReadOnlyList<string> filePaths)
        {
            if (_disposed || WebView.CoreWebView2 is null) return false;
            if (filePaths is null || filePaths.Count == 0) return false;

            try
            {
                // 1. Get the document root node id.
                var docJson = await WebView.CoreWebView2.CallDevToolsProtocolMethodAsync(
                    "DOM.getDocument", "{\"depth\": 0}");

                using var doc = JsonDocument.Parse(docJson);
                if (!doc.RootElement.TryGetProperty("root", out var root) ||
                    !root.TryGetProperty("nodeId", out var rootIdEl))
                    return false;

                var rootNodeId = rootIdEl.GetInt32();

                // 2. Find the first file input inside the page.
                var qsParams = JsonSerializer.Serialize(new
                {
                    nodeId = rootNodeId,
                    selector = "input[type=\"file\"]"
                });

                var qsJson = await WebView.CoreWebView2.CallDevToolsProtocolMethodAsync(
                    "DOM.querySelector", qsParams);

                using var qs = JsonDocument.Parse(qsJson);
                if (!qs.RootElement.TryGetProperty("nodeId", out var nodeIdEl))
                    return false;

                var inputNodeId = nodeIdEl.GetInt32();
                if (inputNodeId == 0) return false;

                // 3. Set the files on the input.
                var setParams = JsonSerializer.Serialize(new
                {
                    nodeId = inputNodeId,
                    files = filePaths
                });

                await WebView.CoreWebView2.CallDevToolsProtocolMethodAsync(
                    "DOM.setFileInputFiles", setParams);

                // 4. Dispatch change + input so the site's listeners fire.
                await WebView.CoreWebView2.ExecuteScriptAsync(@"
                    (function(){
                        var el = document.querySelector('input[type=file]');
                        if (!el) return 'no-el';
                        el.dispatchEvent(new Event('input', { bubbles: true }));
                        el.dispatchEvent(new Event('change', { bubbles: true }));
                        return 'ok';
                    })();
                ");

                return true;
            }
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────
        //  WebView2 events
        // ─────────────────────────────────────────────

        private void OnNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            if (_disposed) return;
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_disposed) return;
                LoadingBar.Visibility = Visibility.Visible;
                HideError();
                RaiseStateChanged(isLoading: true);
            });
        }

        private void OnNavigationCompletedInternal(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            if (_disposed) return;
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_disposed) return;
                LoadingBar.Visibility = Visibility.Collapsed;

                if (args.IsSuccess)
                {
                    HideError();
                    NavigationCompleted?.Invoke(this, sender.Source);
                }
                else
                {
                    ShowError("Unable to load this AI service.",
                        "Please check your connection and try again.");
                }

                RaiseStateChanged(isLoading: false);
            });
        }

        private void OnNewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
        {
            args.Handled = true;
            if (!UrlPolicy.IsWebUrl(args.Uri)) return;
            try
            {
                var browserService = App.GetService<IBrowserService>();
                browserService.OpenInDefaultBrowser(args.Uri);
            }
            catch { }
        }

        private void OnDocumentTitleChanged(CoreWebView2 sender, object args)
        {
            if (_disposed) return;
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_disposed) return;
                TitleChanged?.Invoke(this, sender.DocumentTitle ?? string.Empty);
            });
        }

        private void OnProcessFailed(CoreWebView2 sender, CoreWebView2ProcessFailedEventArgs args)
        {
            if (_disposed) return;
            DispatcherQueue.TryEnqueue(() =>
            {
                if (_disposed) return;
                ShowError("The AI service stopped responding.",
                    "The panel will be reloaded when you continue.");
                RaiseStateChanged(isLoading: false);
            });
        }

        private void RaiseStateChanged(bool isLoading)
        {
            if (_disposed) return;
            NavigationStateChanged?.Invoke(this, new NavigationStateChangedEventArgs
            {
                CanGoBack = WebView.CoreWebView2?.CanGoBack == true,
                CanGoForward = WebView.CoreWebView2?.CanGoForward == true,
                IsLoading = isLoading,
                Url = WebView.CoreWebView2?.Source ?? string.Empty
            });
        }

        private void ShowError(string title, string message)
        {
            if (_disposed) return;
            LoadingBar.Visibility = Visibility.Collapsed;
            ErrorTitle.Text = title;
            ErrorMessage.Text = message;
            ErrorOverlay.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            if (_disposed) return;
            ErrorOverlay.Visibility = Visibility.Collapsed;
        }

        private async void OnRetryClicked(object sender, RoutedEventArgs e)
        {
            if (_disposed) return;
            HideError();
            LoadingBar.Visibility = Visibility.Visible;

            try
            {
                if (WebView.CoreWebView2 is null) return;
                WebView.CoreWebView2.Stop();
                await WebView.CoreWebView2.Profile.ClearBrowsingDataAsync(
                    CoreWebView2BrowsingDataKinds.DiskCache |
                    CoreWebView2BrowsingDataKinds.CacheStorage);

                if (_disposed) return;
                await Task.Delay(300);
                if (_disposed) return;

                var target = CurrentUrl;
                if (!string.IsNullOrEmpty(target) && UrlPolicy.IsWebUrl(target))
                    WebView.CoreWebView2.Navigate(target);
            }
            catch
            {
                if (!_disposed)
                    ShowError("Retry failed.", "Please try again in a moment.");
            }
        }

        private void OnOpenInBrowserClicked(object sender, RoutedEventArgs e)
        {
            if (_disposed) return;
            var url = CurrentUrl;
            if (!string.IsNullOrEmpty(url) && UrlPolicy.IsWebUrl(url))
            {
                try
                {
                    var browserService = App.GetService<IBrowserService>();
                    browserService.OpenInDefaultBrowser(url);
                }
                catch { }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _navCts?.Cancel();
                _navCts?.Dispose();
                _navCts = null;

                if (WebView.CoreWebView2 is not null)
                {
                    WebView.CoreWebView2.NavigationStarting -= OnNavigationStarting;
                    WebView.CoreWebView2.NavigationCompleted -= OnNavigationCompletedInternal;
                    WebView.CoreWebView2.NewWindowRequested -= OnNewWindowRequested;
                    WebView.CoreWebView2.DocumentTitleChanged -= OnDocumentTitleChanged;
                    WebView.CoreWebView2.ProcessFailed -= OnProcessFailed;
                }

                WebView.Close();
            }
            catch { }

            _isInitialized = false;
            _environment = null;
        }
    }
}