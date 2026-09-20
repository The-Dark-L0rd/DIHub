using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DIHub.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace DIHub.APP.Controls
{
    public sealed partial class CommandPalette : UserControl
    {
        private readonly List<CommandItem> _allCommands = new();
        public ObservableCollection<CommandItem> FilteredCommands { get; } = new();

        public event EventHandler? RequestClose;

        public CommandPalette()
        {
            InitializeComponent();
            ResultsList.ItemsSource = FilteredCommands;
        }

        public void SetCommands(IEnumerable<CommandItem> commands)
        {
            _allCommands.Clear();
            _allCommands.AddRange(commands);
        }

        public void Open()
        {
            Visibility = Visibility.Visible;
            SearchBox.Text = string.Empty;
            ApplyFilter(string.Empty);

            DispatcherQueue.TryEnqueue(() =>
            {
                SearchBox.Focus(FocusState.Programmatic);
                if (FilteredCommands.Count > 0)
                    ResultsList.SelectedIndex = 0;
            });
        }

        public void Close()
        {
            Visibility = Visibility.Collapsed;
        }

        // ─────────────────────────────────────────────
        //  Filtering
        // ─────────────────────────────────────────────

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
            => ApplyFilter(SearchBox.Text);

        private void ApplyFilter(string query)
        {
            FilteredCommands.Clear();

            IEnumerable<CommandItem> source = _allCommands;

            if (!string.IsNullOrWhiteSpace(query))
            {
                var q = query.Trim().ToLowerInvariant();
                source = source.Where(c =>
                    Contains(c.Title, q) ||
                    Contains(c.Subtitle, q) ||
                    Contains(c.Keywords, q) ||
                    Contains(c.CategoryLabel, q));
            }

            foreach (var item in source)
                FilteredCommands.Add(item);

            var hasResults = FilteredCommands.Count > 0;

            EmptyText.Visibility = hasResults ? Visibility.Collapsed : Visibility.Visible;
            ResultsList.Visibility = hasResults ? Visibility.Visible : Visibility.Collapsed;

            if (hasResults)
                ResultsList.SelectedIndex = 0;
        }

        private static bool Contains(string? haystack, string needle)
            => !string.IsNullOrEmpty(haystack)
               && haystack.ToLowerInvariant().Contains(needle);

        // ─────────────────────────────────────────────
        //  Keyboard
        // ─────────────────────────────────────────────

        private void OnSearchKeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.Escape:
                    e.Handled = true;
                    RequestClose?.Invoke(this, EventArgs.Empty);
                    break;

                case VirtualKey.Down:
                    e.Handled = true;
                    MoveSelection(+1);
                    break;

                case VirtualKey.Up:
                    e.Handled = true;
                    MoveSelection(-1);
                    break;

                case VirtualKey.Enter:
                    e.Handled = true;
                    ExecuteSelected();
                    break;
            }
        }

        private void MoveSelection(int delta)
        {
            if (FilteredCommands.Count == 0) return;

            var idx = ResultsList.SelectedIndex;
            if (idx < 0) idx = 0;

            var next = Math.Clamp(idx + delta, 0, FilteredCommands.Count - 1);
            ResultsList.SelectedIndex = next;
            ResultsList.ScrollIntoView(ResultsList.SelectedItem);
        }

        // ─────────────────────────────────────────────
        //  Mouse
        // ─────────────────────────────────────────────

        private void OnItemClicked(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is CommandItem cmd)
                Execute(cmd);
        }

        private void OnBackdropTapped(object sender, TappedRoutedEventArgs e)
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private void OnPanelTapped(object sender, TappedRoutedEventArgs e)
        {
            // Prevent the backdrop from closing when user interacts inside the panel.
            e.Handled = true;
        }

        // ─────────────────────────────────────────────
        //  Execution
        // ─────────────────────────────────────────────

        private void ExecuteSelected()
        {
            if (ResultsList.SelectedItem is CommandItem cmd)
                Execute(cmd);
        }

        private void Execute(CommandItem cmd)
        {
            try
            {
                cmd.Execute?.Invoke();
            }
            catch
            {
                // Don't crash if a command throws.
            }
            finally
            {
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}