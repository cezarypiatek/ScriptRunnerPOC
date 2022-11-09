using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

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

        public event EventHandler? OnDirectoryPicked;

        public string DirPath
        {
            get => GetValue(DirPathProperty);
            set => SetValue(DirPathProperty, value);
        }

        private async void ChangeDirClick(object? sender, RoutedEventArgs e)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var sourceWindow = (sender as IControl)?.GetVisualRoot() as Window ?? desktop.MainWindow;

                var dialog = new OpenFolderDialog();
                if (string.IsNullOrWhiteSpace(DirPath) == false && Directory.Exists(DirPath))
                {
                    dialog.Directory = DirPath;
                }

                if (await dialog.ShowAsync(sourceWindow) is { } selectedDirPath)
                {
                    DirPath = selectedDirPath;
                    OnDirectoryPicked?.Invoke(this, new FilePickedArgs(selectedDirPath));
                }
            }
        }
    }
}
