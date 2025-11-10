using Avalonia.Controls;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI;

public class DirectoryPickerControl : IControlRecord
{
    private readonly bool _useWslPathForDirPicker;

    public DirectoryPickerControl(bool useWslPathForDirPicker)
    {
        _useWslPathForDirPicker = useWslPathForDirPicker;
    }

    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        var path = ((DirectoryPicker)Control).DirPath;
        return _useWslPathForDirPicker ? WslPathConverter.ConvertToWslPath(path) : path;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}