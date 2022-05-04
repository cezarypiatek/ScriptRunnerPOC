using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

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
            

        public string FilePath
        {
            get => GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        private void ChangeFileClick(object? sender, RoutedEventArgs e)
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var dialog = new OpenFileDialog();
                if (string.IsNullOrWhiteSpace(FilePath) == false && Path.GetDirectoryName(FilePath) is { } dir)
                {
                    dialog.Directory = dir;
                    dialog.InitialFileName = Path.GetFileName(FilePath);

                }
                
                dialog.AllowMultiple = false;
                
                var result = dialog.ShowAsync(desktop.MainWindow).GetAwaiter().GetResult();
                if (result?.FirstOrDefault() is { } file)
                {
                    FilePath = file;
                }
            }
        }
    }
}
