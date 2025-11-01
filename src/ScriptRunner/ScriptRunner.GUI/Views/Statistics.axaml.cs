using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ScriptRunner.GUI.Views;

public partial class Statistics : UserControl
{
    public Statistics()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

