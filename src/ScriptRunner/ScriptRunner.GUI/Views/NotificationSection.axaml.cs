using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ScriptRunner.GUI.Views;

public partial class NotificationSection : UserControl
{
    public NotificationSection()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}