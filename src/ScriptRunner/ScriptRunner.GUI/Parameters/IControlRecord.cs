using Avalonia.Controls;

namespace ScriptRunner.GUI;

public interface IControlRecord
{
    Control Control { get; set; }

    string GetFormattedValue();

    public string Name { get; set; }

    public bool MaskingRequired { get; set; }
}