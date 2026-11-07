using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace ScriptRunner.GUI.Views
{
    public partial class PasswordBox : UserControl
    {
        private const int ExpiryWarningDays = 7;

        private string? _vaultPassword;
        private DateTimeOffset? _vaultExpiresAt;
        private bool _isApplyingValue;

        public bool HasInvalidVaultReference { get; private set; }

        public PasswordBox()
        {
            InitializeComponent();
            this.FindControl<TextBox>("PasswordTextBox")!.TextChanged += (_, _) =>
            {
                if (_isApplyingValue)
                {
                    return;
                }

                // Avalonia may deliver the notification after a programmatic SetVaultValue has
                // completed. Do not mistake that delayed notification for a manual edit.
                if (VaultKey is not null && Password == _vaultPassword)
                {
                    return;
                }

                HasInvalidVaultReference = false;
                if (VaultKey != null)
                {
                    _vaultPassword = null;
                    _vaultExpiresAt = null;
                    VaultKey = null;
                }
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private string? _vaultKey;

        public string? VaultKey
        {
            get => _vaultKey;
            set
            {
                _vaultKey = value;
                var isBoundToVault = !string.IsNullOrWhiteSpace(value);
                var bindingInfo = this.FindControl<Border>("VaultBindingInfo");
                bindingInfo!.IsVisible = isBoundToVault;
                this.FindControl<TextBlock>("VaultKeyText")!.Text = value;

                var daysUntilExpiry = _vaultExpiresAt is { } expiry
                    ? (int)(expiry.Date - DateTimeOffset.Now.Date).TotalDays
                    : (int?)null;
                var isExpired = isBoundToVault && daysUntilExpiry < 0;
                var isExpiringSoon = isBoundToVault &&
                                     daysUntilExpiry is >= 0 and <= ExpiryWarningDays;
                bindingInfo.Classes.Set("expired", isExpired);
                bindingInfo.Classes.Set("expiringSoon", isExpiringSoon);
                var expiryText = this.FindControl<TextBlock>("VaultExpiryText");
                expiryText!.IsVisible = isExpired || isExpiringSoon;
                expiryText.Text = isExpired
                    ? $"Expired on {_vaultExpiresAt:yyyy-MM-dd}"
                    : daysUntilExpiry switch
                    {
                        0 => $"Expires today ({_vaultExpiresAt:yyyy-MM-dd})",
                        1 => $"Expires in 1 day, on {_vaultExpiresAt:yyyy-MM-dd}",
                        > 1 => $"Expires in {daysUntilExpiry} days, on {_vaultExpiresAt:yyyy-MM-dd}",
                        _ => null
                    };
            }
        }

        public void SetVaultValue(string vaultKey, string? password, DateTimeOffset? expiresAt)
        {
            _isApplyingValue = true;
            try
            {
                HasInvalidVaultReference = false;
                _vaultPassword = password;
                _vaultExpiresAt = expiresAt;
                VaultKey = vaultKey;
                Password = password;
            }
            finally
            {
                _isApplyingValue = false;
            }
        }

        public void SetManualValue(string? password)
        {
            ApplyUnboundValue(password, hasInvalidVaultReference: false);
        }

        public void SetInvalidVaultReference(string? value)
        {
            ApplyUnboundValue(value, hasInvalidVaultReference: true);
        }

        public PasswordBoxState CaptureState() => new(
            Password,
            VaultKey,
            _vaultExpiresAt,
            HasInvalidVaultReference);

        public void RestoreState(PasswordBoxState state)
        {
            if (!string.IsNullOrWhiteSpace(state.VaultKey))
            {
                SetVaultValue(state.VaultKey, state.Password, state.ExpiresAt);
            }
            else if (state.HasInvalidVaultReference)
            {
                SetInvalidVaultReference(state.Password);
            }
            else
            {
                SetManualValue(state.Password);
            }
        }

        private void ApplyUnboundValue(string? value, bool hasInvalidVaultReference)
        {
            _isApplyingValue = true;
            try
            {
                _vaultPassword = null;
                _vaultExpiresAt = null;
                VaultKey = null;
                HasInvalidVaultReference = hasInvalidVaultReference;
                Password = value;
            }
            finally
            {
                _isApplyingValue = false;
            }
        }

        private async void PickFromVault(object? sender, RoutedEventArgs e)
        {
            var pickerDialog = new VaultPicker();
            if (string.IsNullOrWhiteSpace(VaultKey) == false)
            {
                pickerDialog.ViewModel.SelectedEntry = pickerDialog.ViewModel.Entries.FirstOrDefault(x => x.Name == VaultKey);
            }
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var sourceWindow = (sender as Control)?.GetVisualRoot() as Window ?? desktop.MainWindow;
                if (sourceWindow is not null &&
                    await pickerDialog.ShowDialog<VaultEntryChoice>(sourceWindow) is { } choice)
                {
                    SetVaultValue(choice.SelectedEntry.Name!, choice.SelectedEntry.Secret, choice.SelectedEntry.ExpiresAt);
                    OnVaultBindingChanged(new VaultBindingChangedEventArgs(choice));
                }
            }
        }

        private void DetachFromVault(object? sender, RoutedEventArgs e)
        {
            SetManualValue(Password);
        }

        public event EventHandler<VaultBindingChangedEventArgs>? VaultBindingChanged;

        public class VaultBindingChangedEventArgs : EventArgs
        {
            public VaultEntryChoice VaultEntryChoice { get; }

            public VaultBindingChangedEventArgs(VaultEntryChoice vaultEntryChoice)
            {
                VaultEntryChoice = vaultEntryChoice;
            }
        }

        private void OnVaultBindingChanged(VaultBindingChangedEventArgs e) => VaultBindingChanged?.Invoke(this, e);


        public static readonly DirectProperty<PasswordBox, string?> PasswordProperty = AvaloniaProperty.RegisterDirect<PasswordBox, string?>
        (
            name: nameof(Password),
            getter: picker => picker.FindControl<TextBox>("PasswordTextBox")!.Text,
            setter: (picker, s) => picker.FindControl<TextBox>("PasswordTextBox")!.Text = s
        );


        public string? Password
        {
            get => GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }
    }

    public sealed record PasswordBoxState(
        string? Password,
        string? VaultKey,
        DateTimeOffset? ExpiresAt,
        bool HasInvalidVaultReference);
}
