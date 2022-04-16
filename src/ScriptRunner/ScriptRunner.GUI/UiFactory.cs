using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ScriptRunner.GUI.ScriptConfigs;

namespace ScriptRunner.GUI;

public static class UiFactory
{
    public static IEnumerable<IControl> BuildControls(ScriptConfig action)
    {
        var controls = new List<IControl>();

        controls.Add(new Label { Content = action.Name });
        controls.Add(new TextBlock { Text = action.Description });
        controls.Add(new TextBlock { Text = action.Command });

        controls.Add(new Label { Content = "Parameters: " });

        controls.Add(new ParamsPanelFactory().Create(action.Params));

        return controls;
    }
}

public class ParamsPanelFactory
{
    public IPanel Create(IEnumerable<ScriptParam> parameters)
    {
        var paramsPanel = new StackPanel();

        foreach (var param in parameters)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new Label
                    {
                        Content = param.Name
                    },
                    CreateField(param.Prompt)
                }
            };
            
            paramsPanel.Children.Add(stackPanel);
        }

        return paramsPanel;
    }

    private IControl CreateField(PromptType promptType)
    {
        switch (promptType)
        {
            case PromptType.Text:
                //var control = new TextBox();
                //var observer = control.GetObservable(TextBox.TextProperty);
                return new TextBox();
            //case PromptType.Password:
            //    break;
            //case PromptType.Dropdown:
            //    break;
            //case PromptType.Multiselect:
            //    break;
            case PromptType.Datepicker:
                return new DatePicker();
            case PromptType.Checkbox:
                return new CheckBox();
            case PromptType.Multilinetext:
                return new TextBox
                {
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true
                };
            default:
                throw new ArgumentOutOfRangeException(nameof(promptType), promptType, null);
        }
    }
}

public class ControlRecord<T>
{
    public IControl Control { get; set; }

    public IObservable<T> Observer { get; set; }

}