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
    public partial class FilePicker : UserControl
    {
        public FilePicker()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static readonly DirectProperty<FilePicker, string> FilePathProperty = AvaloniaProperty.RegisterDirect<FilePicker, string>(nameof(FilePath), picker => picker.FindControl<TextBox>("FilePathTextBox").Text, (picker, s) => picker.FindControl<TextBox>("FilePathTextBox").Text = s);

        public event EventHandler? OnFilePicked;

        public string FilePath
        {
            get => GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        private async void ChangeFileClick(object? sender, RoutedEventArgs e)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var sourceWindow = (sender as Control)?.GetVisualRoot() as Window ?? desktop.MainWindow;

                var dialog = new OpenFileDialog();
                if (string.IsNullOrWhiteSpace(FilePath) == false && Path.GetDirectoryName(FilePath) is { } dir && Directory.Exists(dir))
                {
                    dialog.Directory = dir;
                    dialog.InitialFileName = Path.GetFileName(FilePath);

                }
                
                dialog.AllowMultiple = false;

                var result = await dialog.ShowAsync(sourceWindow);
                if (result?.FirstOrDefault() is { } file)
                {
                    FilePath = file;
                    OnFilePicked?.Invoke(this, new FilePickedArgs(file));
                }
            }
        }
    }

    public class FilePickedArgs : EventArgs
    {
        public string Path { get; }

        public FilePickedArgs(string path)
        {
            Path = path;
        }
    }
}
