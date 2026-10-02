using System;
using System.Collections.Generic;
using System.Linq;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DIHub.APP.Views
{
    /// <summary>
    /// Result of the Manage Scope dialog. Supports multi-selection:
    /// Global, or a mix of service-level and account-level targets.
    /// </summary>
    public sealed class ScopeSelection
    {
        public bool IsGlobal { get; init; }
        public bool Enabled { get; init; }

        /// <summary>ServiceIds the extension should apply to (all their accounts).</summary>
        public List<string> ServiceIds { get; init; } = new();

        /// <summary>(ServiceId, AccountId) pairs for account-specific targets.</summary>
        public List<(string ServiceId, string AccountId)> Accounts { get; init; } = new();

        public bool IsEmpty =>
            !IsGlobal && ServiceIds.Count == 0 && Accounts.Count == 0;
    }

    public sealed partial class ManageScopeDialog : UserControl
    {
        private ExtensionInfo? _ext;
        private IExtensionManager? _manager;
        private IAIServiceManager? _serviceManager;
        private IAIAccountManager? _accountManager;
        private bool _loaded;

        public ManageScopeDialog()
        {
            InitializeComponent();
        }

        // ─────────────────────────────────────────────
        //  Load
        //  ─────────────────────────────────────────────

        public void Load(ExtensionInfo ext)
        {
            _ext = ext ?? throw new ArgumentNullException(nameof(ext));
            _manager = App.GetService<IExtensionManager>();
            _serviceManager = App.GetService<IAIServiceManager>();
            _accountManager = App.GetService<IAIAccountManager>();

            ExtensionNameText.Text = ext.Name;

            BuildTargetsLists();
            PreselectCurrentScope(ext);
            RefreshCurrentStateText(ext);

            _loaded = true;
            UpdatePanelEnabledState();
        }

        // ─────────────────────────────────────────────
        //  Build checkbox lists
        //  ─────────────────────────────────────────────

        private void BuildTargetsLists()
        {
            ServiceCheckboxList.Children.Clear();
            AccountCheckboxList.Children.Clear();

            if (_serviceManager is null || _accountManager is null) return;

            int serviceCount = 0;
            int accountCount = 0;

            foreach (var svc in _serviceManager.Services)
            {
                // ── Service-level checkbox ──
                var serviceCheck = new CheckBox
                {
                    Content = svc.Name,
                    Tag = svc.Id,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 0)
                };
                serviceCheck.Checked += OnAnyCheckboxChanged;
                serviceCheck.Unchecked += OnAnyCheckboxChanged;
                ServiceCheckboxList.Children.Add(serviceCheck);
                serviceCount++;

                // ── Account-level checkboxes (grouped under this service) ──
                var accounts = _accountManager.GetAccountsForService(svc.Id);
                if (accounts.Count == 0) continue;

                var groupHeader = new TextBlock
                {
                    Text = svc.Name,
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 6, 0, 2),
                    Foreground = (Brush)Application.Current.Resources["AppTextSecondaryBrush"]
                };
                AccountCheckboxList.Children.Add(groupHeader);

                foreach (var acc in accounts)
                {
                    var accCheck = new CheckBox
                    {
                        Content = acc.Name,
                        Tag = (svc.Id, acc.Id),
                        FontSize = 11,
                        Margin = new Thickness(20, 0, 0, 0)
                    };
                    accCheck.Checked += OnAnyCheckboxChanged;
                    accCheck.Unchecked += OnAnyCheckboxChanged;
                    AccountCheckboxList.Children.Add(accCheck);
                    accountCount++;
                }
            }

            ServicesSection.Visibility = serviceCount > 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            AccountsSection.Visibility = accountCount > 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            EmptyTargetsText.Visibility = (serviceCount == 0 && accountCount == 0)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // ─────────────────────────────────────────────
        //  Preselect from existing assignments
        //  ─────────────────────────────────────────────

        private void PreselectCurrentScope(ExtensionInfo ext)
        {
            if (_manager is null) return;

            var existing = _manager.Assignments
                .Where(a => a.ExtensionId == ext.Id)
                .ToList();

            if (existing.Count == 0)
            {
                RadioGlobal.IsChecked = true;
                EnableCheck.IsChecked = true;
                return;
            }

            var serviceAssignments = existing
                .Where(a => a.Scope == ExtensionScope.Service)
                .Select(a => a.ServiceId ?? string.Empty)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet(StringComparer.Ordinal);

            var accountAssignments = existing
                .Where(a => a.Scope == ExtensionScope.Account)
                .Select(a => (a.ServiceId ?? string.Empty, a.AccountId ?? string.Empty))
                .Where(t => !string.IsNullOrEmpty(t.Item1) && !string.IsNullOrEmpty(t.Item2))
                .ToHashSet();

            var globalAssignment = existing.FirstOrDefault(a => a.Scope == ExtensionScope.Global);

            if (serviceAssignments.Count > 0 || accountAssignments.Count > 0)
            {
                RadioSpecific.IsChecked = true;

                foreach (var child in ServiceCheckboxList.Children)
                {
                    if (child is CheckBox cb && cb.Tag is string svcId)
                    {
                        if (serviceAssignments.Contains(svcId))
                            cb.IsChecked = true;
                    }
                }

                foreach (var child in AccountCheckboxList.Children)
                {
                    if (child is CheckBox cb &&
                        cb.Tag is ValueTuple<string, string> t)
                    {
                        if (accountAssignments.Contains((t.Item1, t.Item2)))
                            cb.IsChecked = true;
                    }
                }

                var firstEnabled = existing.FirstOrDefault()?.IsEnabled ?? true;
                EnableCheck.IsChecked = firstEnabled;
            }
            else if (globalAssignment is not null)
            {
                RadioGlobal.IsChecked = true;
                EnableCheck.IsChecked = globalAssignment.IsEnabled;
            }
            else
            {
                RadioGlobal.IsChecked = true;
                EnableCheck.IsChecked = true;
            }
        }

        private void RefreshCurrentStateText(ExtensionInfo ext)
        {
            if (_manager is null) return;

            var anyAccount = _accountManager?.AllAccounts.FirstOrDefault();
            if (anyAccount is null)
            {
                CurrentStateText.Text = "Currently: no account registered";
                return;
            }

            var svc = _serviceManager?.FindService(anyAccount.ServiceId);
            var state = _manager.GetEffectiveState(ext.Id, svc?.Id, anyAccount.Id);

            var scopeLabel = state.Source switch
            {
                ExtensionScope.Account => "Account override",
                ExtensionScope.Service => "Service",
                ExtensionScope.Global => "Global",
                _ => "Default"
            };

            CurrentStateText.Text = state.IsEnabled
                ? $"Currently: Enabled ({scopeLabel})"
                : $"Currently: Disabled ({scopeLabel})";
        }

        // ─────────────────────────────────────────────
        //  Event handlers
        //  ─────────────────────────────────────────────

        private void OnScopeChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            UpdatePanelEnabledState();
        }

        private void OnAnyCheckboxChanged(object sender, RoutedEventArgs e)
        {
            // No-op; state is read on Apply.
        }

        private void UpdatePanelEnabledState()
        {
            // Border doesn't support IsEnabled in WinUI 3 — use the inner ScrollViewer.
            if (TargetsScrollViewer is null) return;
            TargetsScrollViewer.IsEnabled = RadioSpecific.IsChecked == true;
        }

        // ─────────────────────────────────────────────
        //  Get result
        //  ─────────────────────────────────────────────

        public ScopeSelection? GetSelection()
        {
            var enabled = EnableCheck.IsChecked == true;

            if (RadioGlobal.IsChecked == true)
            {
                return new ScopeSelection
                {
                    IsGlobal = true,
                    Enabled = enabled
                };
            }

            var serviceIds = new List<string>();
            var accounts = new List<(string, string)>();

            foreach (var child in ServiceCheckboxList.Children)
            {
                if (child is CheckBox cb &&
                    cb.IsChecked == true &&
                    cb.Tag is string svcId)
                {
                    serviceIds.Add(svcId);
                }
            }

            foreach (var child in AccountCheckboxList.Children)
            {
                if (child is CheckBox cb &&
                    cb.IsChecked == true &&
                    cb.Tag is ValueTuple<string, string> t)
                {
                    accounts.Add((t.Item1, t.Item2));
                }
            }

            if (serviceIds.Count == 0 && accounts.Count == 0)
                return null;

            return new ScopeSelection
            {
                IsGlobal = false,
                Enabled = enabled,
                ServiceIds = serviceIds,
                Accounts = accounts
            };
        }

        // ─────────────────────────────────────────────
        //  Build assignments for the caller
        //  ─────────────────────────────────────────────

        public List<ExtensionAssignment> BuildAssignments(
            string extensionId, ScopeSelection selection)
        {
            var result = new List<ExtensionAssignment>();

            if (selection.IsGlobal)
            {
                result.Add(new ExtensionAssignment
                {
                    ExtensionId = extensionId,
                    Scope = ExtensionScope.Global,
                    IsEnabled = selection.Enabled
                });
                return result;
            }

            foreach (var svcId in selection.ServiceIds)
            {
                result.Add(new ExtensionAssignment
                {
                    ExtensionId = extensionId,
                    Scope = ExtensionScope.Service,
                    ServiceId = svcId,
                    IsEnabled = selection.Enabled
                });
            }

            foreach (var (svcId, accId) in selection.Accounts)
            {
                result.Add(new ExtensionAssignment
                {
                    ExtensionId = extensionId,
                    Scope = ExtensionScope.Account,
                    ServiceId = svcId,
                    AccountId = accId,
                    IsEnabled = selection.Enabled
                });
            }

            return result;
        }
    }
}