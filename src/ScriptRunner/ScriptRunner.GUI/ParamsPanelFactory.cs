using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ScriptRunner.GUI.ScriptConfigs;

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
                        Content = param.Name
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
            //case PromptType.Multiselect:
            //    break;
            case PromptType.Datepicker:
                return new DatePickerControl
                {
                    Control = new DatePicker()
                    {
                        SelectedDate = string.IsNullOrWhiteSpace(p.Default)?null: DateTimeOffset.Parse(p.Default)
                    }
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
        return ((DatePicker)Control).SelectedDate?.DateTime;
    }

    public string Name { get; set; }
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
public class FilePickerControl : IControlRecord
{
    public IControl Control { get; set; }
    public Type ValueType => typeof(string);
    public dynamic GetValue()
    {
        return ((TextBox)Control).Text;
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