using System;
using System.Collections.Generic;
using System.Linq;
using DIHub.Core.Interfaces;
using DIHub.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DIHub.APP.Views
{
    /// <summary>Result of the Manage Scope dialog.</summary>
    public sealed class ScopeSelection
    {
        public ExtensionScope Scope { get; init; }
        public string? ServiceId { get; init; }
        public string? AccountId { get; init; }
        public bool Enabled { get; init; }
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
        // ─────────────────────────────────────────────

        public void Load(ExtensionInfo ext)
        {
            _ext = ext ?? throw new ArgumentNullException(nameof(ext));
            _manager = App.GetService<IExtensionManager>();
            _serviceManager = App.GetService<IAIServiceManager>();
            _accountManager = App.GetService<IAIAccountManager>();

            ExtensionNameText.Text = ext.Name;

            // Populate service combo (Global scope uses none).
            ServiceCombo.Items.Clear();
            ServiceCombo.Items.Add(new ComboBoxItem
            {
                Content = "— Select service —",
                Tag = (string?)null
            });
            foreach (var svc in _serviceManager.Services)
            {
                ServiceCombo.Items.Add(new ComboBoxItem
                {
                    Content = svc.Name,
                    Tag = svc.Id
                });
            }
            ServiceCombo.SelectedIndex = 0;

            // Populate account-service combo.
            AccountServiceCombo.Items.Clear();
            AccountServiceCombo.Items.Add(new ComboBoxItem
            {
                Content = "— Service —",
                Tag = (string?)null
            });
            foreach (var svc in _serviceManager.Services)
            {
                AccountServiceCombo.Items.Add(new ComboBoxItem
                {
                    Content = svc.Name,
                    Tag = svc.Id
                });
            }
            AccountServiceCombo.SelectedIndex = 0;

            // Account combo starts empty.
            AccountCombo.Items.Clear();
            AccountCombo.Items.Add(new ComboBoxItem
            {
                Content = "— Account —",
                Tag = (string?)null
            });
            AccountCombo.SelectedIndex = 0;

            // Pre-select the current assignment scope.
            PreselectCurrentScope(ext);

            // Refresh "currently" text.
            RefreshCurrentStateText(ext);

            _loaded = true;
            UpdateComboStates();
        }

        private void PreselectCurrentScope(ExtensionInfo ext)
        {
            if (_manager is null) return;

            var assignments = _manager.Assignments
                .Where(a => a.ExtensionId == ext.Id)
                .ToList();

            var accAssign = assignments.FirstOrDefault(a => a.Scope == ExtensionScope.Account);
            var svcAssign = assignments.FirstOrDefault(a => a.Scope == ExtensionScope.Service);
            var globAssign = assignments.FirstOrDefault(a => a.Scope == ExtensionScope.Global);

            if (accAssign is not null)
            {
                RadioAccount.IsChecked = true;
                EnableCheck.IsChecked = accAssign.IsEnabled;

                // Pre-select service in account scope combos.
                SelectComboByTag(AccountServiceCombo, accAssign.ServiceId);

                // Populate and select account.
                if (!string.IsNullOrEmpty(accAssign.ServiceId))
                    PopulateAccountCombo(accAssign.ServiceId);
                SelectComboByTag(AccountCombo, accAssign.AccountId);
                return;
            }

            if (svcAssign is not null)
            {
                RadioService.IsChecked = true;
                EnableCheck.IsChecked = svcAssign.IsEnabled;
                SelectComboByTag(ServiceCombo, svcAssign.ServiceId);
                return;
            }

            if (globAssign is not null)
            {
                RadioGlobal.IsChecked = true;
                EnableCheck.IsChecked = globAssign.IsEnabled;
                return;
            }

            // No assignment yet — default to Global enabled.
            RadioGlobal.IsChecked = true;
            EnableCheck.IsChecked = true;
        }

        private void RefreshCurrentStateText(ExtensionInfo ext)
        {
            if (_manager is null) return;

            // Best-effort: resolve for the first available account to
            // show what "effective" means for a real context.
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
        //  Combo helpers
        // ─────────────────────────────────────────────

        private static void SelectComboByTag(ComboBox combo, string? tag)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is ComboBoxItem item &&
                    string.Equals(item.Tag as string, tag, StringComparison.Ordinal))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        private static string? GetComboTag(ComboBox combo)
        {
            if (combo.SelectedItem is ComboBoxItem item)
                return item.Tag as string;
            return null;
        }

        // ─────────────────────────────────────────────
        //  Event handlers
        // ─────────────────────────────────────────────

        private void OnScopeChanged(object sender, RoutedEventArgs e)
        {
            if (!_loaded) return;
            UpdateComboStates();
        }

        private void UpdateComboStates()
        {
            ServiceCombo.IsEnabled = RadioService.IsChecked == true;

            AccountServiceCombo.IsEnabled = RadioAccount.IsChecked == true;
            AccountCombo.IsEnabled = RadioAccount.IsChecked == true
                                     && !string.IsNullOrEmpty(GetComboTag(AccountServiceCombo));
        }

        private void OnAccountServiceChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_loaded) return;

            var svcId = GetComboTag(AccountServiceCombo);
            if (string.IsNullOrEmpty(svcId))
            {
                AccountCombo.Items.Clear();
                AccountCombo.Items.Add(new ComboBoxItem
                {
                    Content = "— Account —",
                    Tag = (string?)null
                });
                AccountCombo.SelectedIndex = 0;
                UpdateComboStates();
                return;
            }

            PopulateAccountCombo(svcId);
            UpdateComboStates();
        }

        private void PopulateAccountCombo(string serviceId)
        {
            if (_accountManager is null) return;

            AccountCombo.Items.Clear();
            AccountCombo.Items.Add(new ComboBoxItem
            {
                Content = "— Account —",
                Tag = (string?)null
            });

            var accounts = _accountManager.GetAccountsForService(serviceId);
            foreach (var acc in accounts)
            {
                AccountCombo.Items.Add(new ComboBoxItem
                {
                    Content = acc.Name,
                    Tag = acc.Id
                });
            }

            AccountCombo.SelectedIndex = 0;
        }

        // ─────────────────────────────────────────────
        //  Get result
        // ─────────────────────────────────────────────

        public ScopeSelection? GetSelection()
        {
            var enabled = EnableCheck.IsChecked == true;

            if (RadioGlobal.IsChecked == true)
            {
                return new ScopeSelection
                {
                    Scope = ExtensionScope.Global,
                    Enabled = enabled
                };
            }

            if (RadioService.IsChecked == true)
            {
                var svcId = GetComboTag(ServiceCombo);
                if (string.IsNullOrEmpty(svcId)) return null;
                return new ScopeSelection
                {
                    Scope = ExtensionScope.Service,
                    ServiceId = svcId,
                    Enabled = enabled
                };
            }

            if (RadioAccount.IsChecked == true)
            {
                var svcId = GetComboTag(AccountServiceCombo);
                var accId = GetComboTag(AccountCombo);
                if (string.IsNullOrEmpty(svcId) || string.IsNullOrEmpty(accId)) return null;
                return new ScopeSelection
                {
                    Scope = ExtensionScope.Account,
                    ServiceId = svcId,
                    AccountId = accId,
                    Enabled = enabled
                };
            }

            return null;
        }
    }
}