using Avalonia.Controls;

namespace ScriptRunner.GUI;

public interface IControlRecord
{
    Control Control { get; set; }

    string GetFormattedValue();

    /// <summary>
    /// Gets a value that can be fed back into <see cref="SetValueFromString"/> to restore the
    /// input. This normally matches the execution value, but content/path controls can retain
    /// their original UI representation here.
    /// </summary>
    string GetValueForInput() => GetFormattedValue();

    bool IsNotEmpty();

    void SetValueFromString(string value);

    public string Name { get; set; }

    public bool MaskingRequired { get; set; }

    public bool Required { get; set; }
}
