using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ScriptRunner.GUI.Views;

public partial class ActionsList : UserControl
{
    public ActionsList()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}