using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI;

public class ParamsPanelFactory
{
    public ParamsPanel Create(IEnumerable<ScriptParam> parameters, Dictionary<string, string> values)
    {
        var paramsPanel = new StackPanel
        {
            Classes = new Classes("paramsPanel"),
        };

        var controlRecords = new List<IControlRecord>();

        foreach (var param in parameters)
        {
            values.TryGetValue(param.Name, out var value);
            var controlRecord = CreateControlRecord(param, value);
            controlRecord.Name = param.Name;
            var actionPanel = new StackPanel
            {
                Children =
                {
                    new Label
                    {
                        Content = string.IsNullOrWhiteSpace(param.Description)? param.Name: param.Description
                    },
                    controlRecord.Control
                },
                Classes = new Classes("paramRow")
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

    private static IControlRecord CreateControlRecord(ScriptParam p, string? value)
    {
        switch (p.Prompt)
        {
            case PromptType.Text:
                return new TextControl
                {
                    Control = new TextBox
                    {
                        Text = value
                    }
                };
            case PromptType.Password:
                return new TextControl
                {
                    Control = new TextBox
                    {
                        PasswordChar = '*',
                        Text = value
                    },
                    MaskingRequired = true
                };
            case PromptType.Dropdown:
                return new DropdownControl
                {
                    Control = new ComboBox
                    { 
                        Items = p.GetPromptSettings("options", out var options) ? options.Split(","):Array.Empty<string>(),
                        SelectedItem = value
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
                        SelectedItems = new AvaloniaList<string>((value ?? string.Empty).Split(delimiter))
                    },
                    Delimiter = delimiter
                };
            case PromptType.Datepicker:
                var yearVisible = p.GetPromptSettings("yearVisible", bool.Parse, true);
                var monthVisible = p.GetPromptSettings("monthVisible", bool.Parse, true);
                var dayVisible = p.GetPromptSettings("dayVisible", bool.Parse, true);
                DateTimeOffset? selectedDate = string.IsNullOrWhiteSpace(value)?(p.GetPromptSettings("todayAsDefault", bool.Parse, false)? DateTimeOffset.Now.Date:null) : DateTimeOffset.Parse(value);
                return new DatePickerControl
                {
                    Control = yearVisible && monthVisible && dayVisible? 
                            new CalendarDatePicker
                            {
                                SelectedDate = selectedDate?.Date,
                                IsTodayHighlighted = true
                            }
                            : new DatePicker
                            {
                                SelectedDate = selectedDate,
                                YearVisible = yearVisible,
                                MonthVisible = monthVisible,
                                DayVisible = dayVisible,
                                
                            },
                    Format = p.GetPromptSettings("format", out var format) ? format : null,
                };
            case PromptType.TimePicker:
                return new TimePickerControl
                {
                    Control = new TimePicker
                    {
                        SelectedTime = string.IsNullOrWhiteSpace(value)?null: TimeSpan.Parse(value),
                        ClockIdentifier = "24HourClock"
                    },
                    Format = p.GetPromptSettings("format", out var timeFormat) ? timeFormat : null,
                };
            case PromptType.Checkbox:
                var checkedValueText  = p .GetPromptSettings("checkedValue", out var checkedValue)? checkedValue: "true";
                return new CheckboxControl
                {
                    Control = new CheckBox
                    {
                        IsChecked = string.IsNullOrWhiteSpace(value) == false && value == checkedValueText
                    },
                    CheckedValue = checkedValueText,
                    UncheckedValue =  p.GetPromptSettings("uncheckedValue", out var uncheckedValue)? uncheckedValue: "false",
                };
            case PromptType.Multilinetext:
                return new TextControl
                {
                    Control = new TextBox
                    {
                        TextWrapping = TextWrapping.Wrap,
                        AcceptsReturn = true,
                        Height = 100,
                        Text = value
                    }
                };
            case PromptType.FileContent:
                return new FileContent(p.GetPromptSettings("extension", out var extension)?extension:"dat")
                {
                    Control = new TextBox
                    {
                        TextWrapping = TextWrapping.Wrap,
                        AcceptsReturn = true,
                        Height = 100,
                        Text = File.Exists(value)? File.ReadAllText(value): string.Empty
                    }
                };
            case PromptType.FilePicker:
                return new FilePickerControl
                {
                    Control = new FilePicker
                    {
                        FilePath = value
                    }
                };
            case PromptType.DirectoryPicker:
                return new DirectoryPickerControl
                {
                    Control = new DirectoryPicker
                    {
                        DirPath = value
                    }
                };
            case PromptType.Numeric:
                return new NumericControl
                {
                    Control = new NumericUpDown{
                        Value = double.TryParse(value, out var valueDouble)? valueDouble: 0,
                        Minimum = p.GetPromptSettings("min", out var minValue) && double.TryParse(minValue, out var mindDouble)? mindDouble : double.MinValue,
                        Maximum = p.GetPromptSettings("max", out var maxValue) && double.TryParse(maxValue, out var maxDouble)? maxDouble: double.MaxValue,
                        Increment = p.GetPromptSettings("step", out var stepValue) && double.TryParse(stepValue, out var stepDouble)? stepDouble: 1.0,
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
    public string CheckedValue { get; set; } = "true";
    public string UncheckedValue { get; set; } = "false";
    public string GetFormattedValue()
    {
        return ((CheckBox)Control).IsChecked == true ? CheckedValue: UncheckedValue;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}

public class DatePickerControl : IControlRecord
{
    public IControl Control { get; set; }

    public string GetFormattedValue()
    {
        var selectedDateTime =  Control switch
        {
            DatePicker dp => dp.SelectedDate?.DateTime,
            CalendarDatePicker cdp => cdp.SelectedDate?.Date,
            _ => null
        };
        if (string.IsNullOrWhiteSpace(Format) == false && selectedDateTime is {} value)
        {
            return value.ToString(Format);
        }
        return selectedDateTime?.ToString() ?? string.Empty;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }

    public string? Format { get; set; }
}

public class TimePickerControl : IControlRecord
{
    public IControl Control { get; set; }

    public string GetFormattedValue()
    {
        var selectedTime = ((TimePicker)Control).SelectedTime;
        if (string.IsNullOrWhiteSpace(Format) == false && selectedTime is {} value)
        {
            return value.ToString(Format);
        }
        return selectedTime?.ToString() ?? string.Empty;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }

    public string? Format { get; set; }
}

public class DropdownControl : IControlRecord
{
    public IControl Control { get; set; }

    public string GetFormattedValue()
    {
        return ((ComboBox)Control).SelectedItem?.ToString();
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}

public class TextControl : IControlRecord
{
    public IControl Control { get; set; }

    public string GetFormattedValue()
    {
        return ((TextBox)Control).Text;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}
public class FileContent : IControlRecord
{
    public IControl Control { get; set; }
    public string FileName { get; set; }

    public FileContent(string extension)
    {
        FileName = Path.GetTempFileName() + "." + extension;
    }

    public string GetFormattedValue()
    {
        var fileContent = ((TextBox)Control).Text;
        File.WriteAllText(FileName, fileContent, Encoding.UTF8);
        return FileName;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}

public class MultiSelectControl : IControlRecord
{
    public IControl Control { get; set; }

    public string GetFormattedValue()
    {
        var selectedItems = ((ListBox)Control).SelectedItems;
        var copy = new List<string>();
        foreach (var item in selectedItems)
        {
            if (item.ToString() is { } nonNullItem)
            {
                copy.Add(nonNullItem);
            }
        }

        return string.Join(Delimiter, copy);
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
    public string Delimiter { get; set; }

}
public class FilePickerControl : IControlRecord
{
    public IControl Control { get; set; }

    public string GetFormattedValue()
    {
        return ((FilePicker)Control).FilePath;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}

public class DirectoryPickerControl : IControlRecord
{
    public IControl Control { get; set; }

    public string GetFormattedValue()
    {
        return ((DirectoryPicker)Control).DirPath;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}

public class NumericControl : IControlRecord
{
    public IControl Control { get; set; }

    public string GetFormattedValue()
    {
        return ((NumericUpDown)Control).Text;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}

public interface IControlRecord
{
    IControl Control { get; set; }

    string GetFormattedValue();

    public string Name { get; set; }

    public bool MaskingRequired { get; set; }
}