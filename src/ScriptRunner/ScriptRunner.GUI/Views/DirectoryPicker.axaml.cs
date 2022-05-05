using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ScriptRunner.GUI.Views
{
    public partial class DirectoryPicker : UserControl
    {
        public DirectoryPicker()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }


        public static readonly DirectProperty<DirectoryPicker, string> DirPathProperty = AvaloniaProperty.RegisterDirect<DirectoryPicker, string>(nameof(DirPath), picker => picker.FindControl<TextBox>("DirPathTextBox").Text, (picker, s) => picker.FindControl<TextBox>("DirPathTextBox").Text = s);


        public string DirPath
        {
            get => GetValue(DirPathProperty);
            set => SetValue(DirPathProperty, value);
        }

        private void ChangeDirClick(object? sender, RoutedEventArgs e)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var dialog = new OpenFolderDialog();
                if (string.IsNullOrWhiteSpace(DirPath) == false)
                {
                    dialog.Directory = DirPath;
                }

                if (dialog.ShowAsync(desktop.MainWindow).GetAwaiter().GetResult() is { } selectedDirPath)
                {
                    DirPath = selectedDirPath;
                }
            }
        }
    }
}
