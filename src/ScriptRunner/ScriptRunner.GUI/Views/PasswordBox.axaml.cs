using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ScriptRunner.GUI.Views
{
    public partial class PasswordBox : UserControl
    {
        public PasswordBox()
        {
            InitializeComponent();
            this.FindControl<TextBox>("PasswordTextBox").AddHandler(TextInputEvent, (sender, args) =>
            {
                VaultKey = null;
            }, RoutingStrategies.Tunnel);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public string VaultKey { get; set; }

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
                    VaultKey = choice.SelectedEntry.Name;
                    OnVaultBindingChanged(new VaultBindingChangedEventArgs(choice));
                    Dispatcher.UIThread.Post(() =>
                    {
                        Password = choice.SelectedEntry.Secret;
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
