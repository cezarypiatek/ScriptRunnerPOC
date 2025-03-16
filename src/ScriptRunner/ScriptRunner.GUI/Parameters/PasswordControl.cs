using Avalonia.Controls;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI;

public class PasswordControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((PasswordBox)Control).Password;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}