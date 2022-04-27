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
            var controlRecord = CreateControlRecord(param.Prompt);
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

    private static IControlRecord CreateControlRecord(PromptType promptType)
    {
        switch (promptType)
        {
            case PromptType.Text:
                var control = new TextBox();
                return new TextControl
                {
                    Control = control
                };
            case PromptType.Password:
                return new TextControl
                {
                    Control = new TextBox
                    {
                        PasswordChar = '*'
                    }
                };
            case PromptType.Dropdown:
                return new DropdownControl
                {
                    Control = new ComboBox
                    { 
                        Items = new List<string>
                        {
                            "SomeItem1",
                            "SomeItem2"
                        }
                    }
                };
            //case PromptType.Multiselect:
            //    break;
            case PromptType.Datepicker:
                return new DatePickerControl
                {
                    Control = new DatePicker()
                };
            case PromptType.Checkbox:
                return new CheckboxControl
                {
                    Control = new CheckBox()
                };
            case PromptType.Multilinetext:
                return new TextControl
                {
                    Control = new TextBox
                    {
                        TextWrapping = TextWrapping.Wrap,
                        AcceptsReturn = true,
                        Height = 60
                    }
        };
            default:
                throw new ArgumentOutOfRangeException(nameof(promptType), promptType, null);
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
    public Type ValueType => typeof(bool);
    public dynamic GetValue()
    {
        return ((CheckBox)Control).IsChecked ?? false;
    }
}

public class DatePickerControl : IControlRecord
{
    public IControl Control { get; set; }
    public Type ValueType => typeof(DateTime);
    public dynamic GetValue()
    {
        return ((DatePicker)Control).SelectedDate?.DateTime;
    }
}

public class DropdownControl : IControlRecord
{
    public IControl Control { get; set; }
    public Type ValueType => typeof(string);
    public dynamic GetValue()
    {
        return ((ComboBox)Control).SelectedItem?.ToString();
    }
}

public class TextControl : IControlRecord
{
    public IControl Control { get; set; }
    public Type ValueType => typeof(string);
    public dynamic GetValue()
    {
        return ((TextBox)Control).Text;
    }
}

public interface IControlRecord
{
    IControl Control { get; set; }

    Type ValueType { get; }

    dynamic GetValue();
}