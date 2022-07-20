using System;
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
        public PasswordBox()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void PickFromVault(object? sender, RoutedEventArgs e)
        {
            var pickerDialog = new VaultPicker();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var sourceWindow = (sender as IControl)?.GetVisualRoot() as Window ?? desktop.MainWindow;
                var selectedPassword = await pickerDialog.ShowDialog<string?>(sourceWindow);
                if (selectedPassword != null)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        Password = selectedPassword;
                    });

                }
            }
        }

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
