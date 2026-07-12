using Avalonia.Controls;

namespace ScriptRunner.GUI;

public interface IControlRecord
{
    Control Control { get; set; }

    string GetFormattedValue();

    bool IsNotEmpty();

    void SetValueFromString(string value);

    public string Name { get; set; }

    public bool MaskingRequired { get; set; }

    public bool Required { get; set; }
}
