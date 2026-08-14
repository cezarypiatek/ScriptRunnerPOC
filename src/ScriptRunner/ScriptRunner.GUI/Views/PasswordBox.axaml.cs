using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ScriptRunner.GUI.Views
{
    public partial class PasswordBox : UserControl
    {
        private string? _vaultPassword;
        private DateTimeOffset? _vaultExpiresAt;

        public PasswordBox()
        {
            InitializeComponent();
            this.FindControl<TextBox>("PasswordTextBox").TextChanged += (_, _) =>
            {
                if (VaultKey != null && Password != _vaultPassword)
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
                bindingInfo.IsVisible = isBoundToVault;
                this.FindControl<TextBlock>("VaultKeyText").Text = value;

                var isExpired = isBoundToVault &&
                                _vaultExpiresAt is { } expiry &&
                                expiry.Date < DateTimeOffset.Now.Date;
                bindingInfo.Classes.Set("expired", isExpired);
                var expiryText = this.FindControl<TextBlock>("VaultExpiryText");
                expiryText.IsVisible = isExpired;
                expiryText.Text = isExpired ? $"Expired on {_vaultExpiresAt:yyyy-MM-dd}" : null;
            }
        }

        public void SetVaultValue(string vaultKey, string? password, DateTimeOffset? expiresAt)
        {
            _vaultPassword = password;
            _vaultExpiresAt = expiresAt;
            Password = password;
            VaultKey = vaultKey;
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
                if (await pickerDialog.ShowDialog<VaultEntryChoice>(sourceWindow) is { } choice)
                {
                    OnVaultBindingChanged(new VaultBindingChangedEventArgs(choice));
                    Dispatcher.UIThread.Post(() =>
                    {
                        SetVaultValue(choice.SelectedEntry.Name!, choice.SelectedEntry.Secret, choice.SelectedEntry.ExpiresAt);
                    });
                }
            }
        }

        public event EventHandler<VaultBindingChangedEventArgs> VaultBindingChanged;

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
            getter: picker => picker.FindControl<TextBox>("PasswordTextBox").Text,
            setter: (picker, s) => picker.FindControl<TextBox>("PasswordTextBox").Text = s
        );


        public string? Password
        {
            get => GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }
    }
}
