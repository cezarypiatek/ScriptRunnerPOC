namespace ScriptRunner.GUI.ScriptConfigs;

/// <summary>
/// Represents a dropdown or multiselect option with a display label and value
/// </summary>
public class DropdownOption
{
    public string Label { get; set; }
    public string Value { get; set; }

    public DropdownOption(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public DropdownOption(string value)
    {
        Label = value;
        Value = value;
    }

    public override string ToString() => Label;
}

