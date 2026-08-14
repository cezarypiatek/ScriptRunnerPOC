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

        public PasswordBox()
        {
            InitializeComponent();
            this.FindControl<TextBox>("PasswordTextBox").TextChanged += (_, _) =>
            {
                if (VaultKey != null && Password != _vaultPassword)
                {
                    _vaultPassword = null;
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
                if (_vaultKey == value)
                {
                    return;
                }

                _vaultKey = value;
                var isBoundToVault = !string.IsNullOrWhiteSpace(value);
                this.FindControl<Border>("VaultBindingInfo").IsVisible = isBoundToVault;
                this.FindControl<TextBlock>("VaultKeyText").Text = value;
            }
        }

        public void SetVaultValue(string vaultKey, string? password)
        {
            _vaultPassword = password;
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
                        SetVaultValue(choice.SelectedEntry.Name, choice.SelectedEntry.Secret);
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
