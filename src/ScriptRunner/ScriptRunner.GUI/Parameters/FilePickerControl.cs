using Avalonia.Controls;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI;

public class FilePickerControl : IControlRecord
{
    private readonly bool _useWslPathFormat;
    public Control Control { get; set; }

    public FilePickerControl(bool useWslPathFormat)
    {
        _useWslPathFormat = useWslPathFormat;
    }

    public string GetFormattedValue()
    {
        var path = ((FilePicker)Control).FilePath;
        return _useWslPathFormat ? WslPathConverter.ConvertToWslPath(path) : path;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}