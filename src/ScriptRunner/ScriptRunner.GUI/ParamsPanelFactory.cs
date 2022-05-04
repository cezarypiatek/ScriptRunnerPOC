using System;
using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI;

public class ParamsPanelFactory
{
    public ParamsPanel Create(IEnumerable<ScriptParam> parameters)
    {
        var paramsPanel = new StackPanel();
        var controlRecords = new List<IControlRecord>();

        foreach (var param in parameters)
        {
            var controlRecord = CreateControlRecord(param);
            controlRecord.Name = param.Name;
            var actionPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new Label
                    {
                        Content = string.IsNullOrWhiteSpace(param.Description)? param.Name: param.Description
                    },
                    controlRecord.Control
                }
            };
            
            paramsPanel.Children.Add(actionPanel);
            controlRecords.Add(controlRecord);
        }

        return new ParamsPanel
        {
            Panel = paramsPanel,
            ControlRecords = controlRecords
        };
    }

    private static IControlRecord CreateControlRecord(ScriptParam p)
    {
        switch (p.Prompt)
        {
            case PromptType.Text:
                return new TextControl
                {
                    Control = new TextBox()
                    {
                        Text = p.Default
                    }
                };
            case PromptType.Password:
                return new TextControl
                {
                    Control = new TextBox
                    {
                        PasswordChar = '*',
                        Text = p.Default
                    }
                };
            case PromptType.Dropdown:
                return new DropdownControl
                {
                    Control = new ComboBox
                    { 
                        Items = p.GetPromptSettings("options", out var options) ? options.Split(","):Array.Empty<string>(),
                        SelectedItem = p.Default
                    }
                };
            case PromptType.Multiselect:
                var delimiter = p.GetPromptSettings("delimiter", s => s, ",");
                return new MultiSelectControl
                {
                    Control = new ListBox
                    {
                        SelectionMode = SelectionMode.Multiple,
                        Items = p.GetPromptSettings("options", out var multiSelectOptions) ? multiSelectOptions.Split(delimiter) : Array.Empty<string>(),
                        SelectedItems = new AvaloniaList<string>((p.Default ?? string.Empty).Split(delimiter))
                    },
                    Delimiter = delimiter
                };
            case PromptType.Datepicker:
                return new DatePickerControl
                {
                    Control = new DatePicker()
                    {
                        SelectedDate = string.IsNullOrWhiteSpace(p.Default)?null: DateTimeOffset.Parse(p.Default),
                        YearVisible = p.GetPromptSettings("yearVisible", bool.Parse, true),
                        MonthVisible = p.GetPromptSettings("monthVisible", bool.Parse, true),
                        DayVisible = p.GetPromptSettings("dayVisible", bool.Parse, true),
                    },
                    Format = p.GetPromptSettings("format", out var format) ? format : null,
                };
            case PromptType.Checkbox:
                return new CheckboxControl
                {
                    Control = new CheckBox(),
                    CheckedValue = p.GetPromptSettings("checkedValue", out var checkedValue)? checkedValue: "true",
                    UncheckedValue =  p.GetPromptSettings("uncheckedValue", out var uncheckedValue)? uncheckedValue: "false",
                };
            case PromptType.Multilinetext:
                return new TextControl
                {
                    Control = new TextBox
                    {
                        TextWrapping = TextWrapping.Wrap,
                        AcceptsReturn = true,
                        Height = 60,
                        Text = p.Default
                    }
                };
            case PromptType.FilePicker:
                return new FilePickerControl
                {
                    Control = new FilePicker
                    {
                        FilePath = p.Default
                    }
                };
            case PromptType.DirectoryPicker:
                return new DirectoryPickerControl
                {
                    Control = new DirectoryPicker()
                    {
                        DirPath = p.Default
                    }
                };
            default:
                throw new ArgumentOutOfRangeException(nameof(p.Prompt), p.Prompt, null);
        }
    }
}


public class ParamsPanel
{
    public IPanel Panel { get; set; }

    public IEnumerable<IControlRecord> ControlRecords { get; set; }
}

public class CheckboxControl : IControlRecord
{
    public IControl Control { get; set; }
    public Type ValueType => typeof(string);
    public string CheckedValue { get; set; } = "true";
    public string UncheckedValue { get; set; } = "false";
    public dynamic GetValue()
    {
        return ((CheckBox)Control).IsChecked == true ? CheckedValue: UncheckedValue;
    }

    public string Name { get; set; }
}

public class DatePickerControl : IControlRecord
{
    public IControl Control { get; set; }
    public Type ValueType => typeof(DateTime);
    public dynamic GetValue()
    {
        var selectedDateTime = ((DatePicker)Control).SelectedDate?.DateTime;
        if (string.IsNullOrWhiteSpace(Format) == false && selectedDateTime is {} value)
        {
            return value.ToString(Format);
        }
        return selectedDateTime;
    }

    public string Name { get; set; }

    public string? Format { get; set; }
}

public class DropdownControl : IControlRecord
{
    public IControl Control { get; set; }
    public Type ValueType => typeof(string);
    public dynamic GetValue()
    {
        return ((ComboBox)Control).SelectedItem?.ToString();
    }

    public string Name { get; set; }
}

public class TextControl : IControlRecord
{
    public IControl Control { get; set; }
    public Type ValueType => typeof(string);
    public dynamic GetValue()
    {
        return ((TextBox)Control).Text;
    }

    public string Name { get; set; }
}

public class MultiSelectControl : IControlRecord
{
    public IControl Control { get; set; }
    public Type ValueType => typeof(string);
    public dynamic GetValue()
    {
        return string.Join(Delimiter, ((ListBox)Control).SelectedItems);
    }

    public string Name { get; set; }
    public string Delimiter { get; set; }

}
public class FilePickerControl : IControlRecord
{
    public IControl Control { get; set; }
    public Type ValueType => typeof(string);
    public dynamic GetValue()
    {
        return ((FilePicker)Control).FilePath;
    }

    public string Name { get; set; }
}

public class DirectoryPickerControl : IControlRecord
{
    public IControl Control { get; set; }
    public Type ValueType => typeof(string);
    public dynamic GetValue()
    {
        return ((DirectoryPicker)Control).DirPath;
    }

    public string Name { get; set; }
}

public interface IControlRecord
{
    IControl Control { get; set; }

    Type ValueType { get; }

    dynamic GetValue();

    public string Name { get; set; }

}