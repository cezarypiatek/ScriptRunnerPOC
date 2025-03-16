using Avalonia.Controls;

namespace ScriptRunner.GUI;

public class NumericControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((NumericUpDown)Control).Text;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}