using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Projektanker.Icons.Avalonia;
using ScriptRunner.GUI.Infrastructure;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.ViewModels;
using ScriptRunner.GUI.Views;

namespace ScriptRunner.GUI;

public class ParamsPanelFactory
{
    private readonly VaultProvider _vaultProvider;

    public ParamsPanelFactory(VaultProvider vaultProvider)
    {
        _vaultProvider = vaultProvider;
    }
    
    public ParamsPanel Create(ScriptConfig action, Dictionary<string, string> values, Func<string, string, Task<string?>> commandExecutor)
    {
        var paramsPanel = new StackPanel
        {
            Classes =
            {
                "paramsPanel"
            }
        };

        var controlRecords = new List<IControlRecord>();
        var appSettings = AppSettingsService.Load();
        var secretBindings = appSettings.VaultBindings ?? new List<VaultBinding>();
        foreach (var (param,i) in action.Params.Select((x,i)=>(x,i)))
        {
            values.TryGetValue(param.Name, out var value);
            var controlRecord = CreateControlRecord(param, value, i, action, secretBindings);
            controlRecord.Name = param.Name;
            if (controlRecord.Control is Layoutable l)
            {
                l.MaxWidth = 500;
            }

            var label = new Label
            {
                Content = string.IsNullOrWhiteSpace(param.Description)? param.Name: param.Description,
                        
            };
            ToolTip.SetTip(label, param.Name);
            var actionPanel = new StackPanel
            {
                Children =
                {
                    label,
                    controlRecord.Control
                },
                Classes =
                {
                    "paramRow"
                }
            };
            if (string.IsNullOrWhiteSpace(param.ValueGeneratorCommand) == false)
            {
                var generateButton = new Button()
                {
                    Margin = new(5,0,5,0),
                    Width = 50,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };
                generateButton.Click += async(sender, args) =>
                {
                    generateButton.IsEnabled = false;
                    generateButton.Classes.Add("spinning");
                    var result = await commandExecutor($"Generate parameter for '{param.Name}'", param.ValueGeneratorCommand);
                    Dispatcher.UIThread.Post(() =>
                    {
                        //TODO: Handle other controls
                        if (controlRecord is { Control: TextBox tb })
                        {
                            tb.Text = result?.Trim() ?? string.Empty;
                            generateButton.Classes.Remove("spinning");
                            generateButton.IsEnabled = true;
                        }
                    });
                };
                Attached.SetIcon(generateButton, "fas fa-wand-magic-sparkles");
                ToolTip.SetTip(generateButton, "Auto fill");
                actionPanel.Children.Add(generateButton);
            }
            paramsPanel.Children.Add(actionPanel);
            controlRecords.Add(controlRecord);
        }

        return new ParamsPanel
        {
            Panel = paramsPanel,
            ControlRecords = controlRecords
        };
    }

    private IControlRecord CreateControlRecord(ScriptParam p, string? value, int index,
        ScriptConfig scriptConfig, List<VaultBinding> secretBindings)
    {
        switch (p.Prompt)
        {
            case PromptType.Text:
                return new TextControl
                {
                    Control = new TextBox
                    {
                        Text = value,
                        TabIndex = index,
                        IsTabStop = true,
                        Width = 500
                    }
                };
            case PromptType.Password:

                var passwordBox = new PasswordBox
                {
                    TabIndex = index,
                    IsTabStop = true,
                    Width = 500
                };

                var vaultKey = value?.StartsWith(MainWindowViewModel.VaultReferencePrefix) == true
                        ? value.Substring(MainWindowViewModel.VaultReferencePrefix.Length)
                        : secretBindings.FirstOrDefault(x => x.ActionName == scriptConfig.Name && x.ParameterName == p.Name)?.VaultKey;
                
                if (string.IsNullOrWhiteSpace(vaultKey) == false )
                {
                    var vaultEntries = _vaultProvider.ReadFromVault();
                    if (vaultEntries.FirstOrDefault(x => x.Name == vaultKey) is { } vaultEntry)
                    {
                        passwordBox.VaultKey = vaultEntry.Name;
                        passwordBox.Password = vaultEntry.Secret;
                    }
                }
                else
                {
                    passwordBox.Password = value;
                }
                
                passwordBox.VaultBindingChanged += (sender, args) =>
                {
                    if (args.VaultEntryChoice.RememberBinding)
                    {
                        AppSettingsService.UpdateVaultBindings(new VaultBinding
                        {
                            ActionName = scriptConfig.Name,
                            ParameterName = p.Name,
                            VaultKey = args.VaultEntryChoice.SelectedEntry.Name
                        });
                    }
                };
                return new PasswordControl
                {
                    Control = passwordBox,
                    MaskingRequired = true,
                };
            case PromptType.Dropdown:
                return new DropdownControl
                {
                    Control = new ComboBox
                    { 
                        ItemsSource = p.GetPromptSettings("options", out var options) ? options.Split(","):Array.Empty<string>(),
                        SelectedItem = value,
                        TabIndex = index,
                        IsTabStop = true
                    }
                };
            case PromptType.Multiselect:
                var delimiter = p.GetPromptSettings("delimiter", s => s, ",");
                return new MultiSelectControl
                {
                    Control = new ListBox
                    {
                        SelectionMode = SelectionMode.Multiple,
                        ItemsSource = p.GetPromptSettings("options", out var multiSelectOptions) ? multiSelectOptions.Split(delimiter) : Array.Empty<string>(),
                        SelectedItems = new AvaloniaList<string>((value ?? string.Empty).Split(delimiter)),
                        TabIndex = index,
                        IsTabStop = true
                    },
                    Delimiter = delimiter
                };
            case PromptType.Datepicker:
                var yearVisible = p.GetPromptSettings("yearVisible", bool.Parse, true);
                var monthVisible = p.GetPromptSettings("monthVisible", bool.Parse, true);
                var dayVisible = p.GetPromptSettings("dayVisible", bool.Parse, true);
                var culture = p.GetPromptSettings("culture", CultureInfo.GetCultureInfo, CultureInfo.CurrentCulture);
                DateTimeOffset? selectedDate = string.IsNullOrWhiteSpace(value)?(p.GetPromptSettings("todayAsDefault", bool.Parse, false)? DateTimeOffset.Now.Date:null) : DateTimeOffset.Parse(value, culture);
                return new DatePickerControl
                {
                    Culture = culture,
                    Control = yearVisible && monthVisible && dayVisible? 
                            new CalendarDatePicker
                            {
                                SelectedDate = selectedDate?.Date,
                                IsTodayHighlighted = true,
                                TabIndex = index,
                                IsTabStop = true,
                                FirstDayOfWeek = DayOfWeek.Monday
                            }
                            : new DatePicker
                            {
                                SelectedDate = selectedDate,
                                YearVisible = yearVisible,
                                MonthVisible = monthVisible,
                                DayVisible = dayVisible,
                                TabIndex = index,
                                IsTabStop = true

                            },
                    Format = p.GetPromptSettings("format", out var format) ? format : null,
                };
            case PromptType.TimePicker:
                return new TimePickerControl
                {
                    Control = new TimePicker
                    {
                        SelectedTime = string.IsNullOrWhiteSpace(value)?null: TimeSpan.Parse(value),
                        ClockIdentifier = "24HourClock",
                        TabIndex = index,
                        IsTabStop = true
                    },
                    Format = p.GetPromptSettings("format", out var timeFormat) ? timeFormat : null,
                };
            case PromptType.Checkbox:
                var checkedValueText  = p.GetPromptSettings("checkedValue", out var checkedValue)? checkedValue: "true";
                return new CheckboxControl
                {
                    Control = new CheckBox
                    {
                        IsChecked = string.IsNullOrWhiteSpace(value) == false && value == checkedValueText,
                        TabIndex = index,
                        IsTabStop = true
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
                        Text = value,
                        TabIndex = index,
                        IsTabStop = true,
                        Width = 500
                    }
                };
            case PromptType.FileContent:
                if (string.IsNullOrWhiteSpace(value) == false && Path.IsPathRooted(value) == false)
                {
                    value = Path.GetFullPath(value, scriptConfig.WorkingDirectory);
                }
                
                
                var templateText  = p.GetPromptSettings("templateText", out var rawTemplate)? rawTemplate: "";
                var textForControl = File.Exists(value) ? File.ReadAllText(value) : templateText;
                
                return new FileContent(p.GetPromptSettings("extension", out var extension)?extension:"dat")
                {
                    Control = new TextBox
                    {
                        TextWrapping = TextWrapping.Wrap,
                        AcceptsReturn = true,
                        Height = 100,
                        Text = textForControl,
                        TabIndex = index,
                        IsTabStop = true,
                        Width = 500
                    }
                };
            case PromptType.FilePicker:

                if (string.IsNullOrWhiteSpace(value)== false && Path.IsPathRooted(value) == false)
                {
                    value = Path.GetFullPath(value, scriptConfig.WorkingDirectory);
                }
                
                return new FilePickerControl
                {
                    Control = new FilePicker
                    {
                        FilePath = value,
                        TabIndex = index,
                        IsTabStop = true,
                        Width = 500
                    }
                };
            case PromptType.DirectoryPicker:
                if (string.IsNullOrWhiteSpace(value)== false && Path.IsPathRooted(value) == false)
                {
                    value = Path.GetFullPath(value, scriptConfig.WorkingDirectory);
                }
                return new DirectoryPickerControl
                {
                    Control = new DirectoryPicker
                    {
                        DirPath = value,
                        TabIndex = index,
                        IsTabStop = true,
                        Width = 500
                    }
                };
            case PromptType.Numeric:
                return new NumericControl
                {
                    Control = new NumericUpDown{
                        Value = decimal.TryParse(value, out var valueDouble)? valueDouble: 0,
                        Minimum = p.GetPromptSettings("min", out var minValue) && decimal.TryParse(minValue, out var mindDouble)? mindDouble : decimal.MinValue,
                        Maximum = p.GetPromptSettings("max", out var maxValue) && decimal.TryParse(maxValue, out var maxDouble)? maxDouble: decimal.MaxValue,
                        Increment = p.GetPromptSettings("step", out var stepValue) && decimal.TryParse(stepValue, out var stepDouble)? (int)stepDouble: 1,
                        ParsingNumberStyle = NumberStyles.Integer,
                        TabIndex = index,
                        IsTabStop = true
                    }
                };
            default:
                throw new ArgumentOutOfRangeException(nameof(p.Prompt), p.Prompt, null);
        }
    }
}


public class ParamsPanel
{
    public Panel Panel { get; set; }

    public IEnumerable<IControlRecord> ControlRecords { get; set; }
}

public class CheckboxControl : IControlRecord
{
    public Control Control { get; set; }
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
    public Control Control { get; set; }

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
            return value.ToString(Format, Culture);
        }
        return selectedDateTime?.ToString() ?? string.Empty;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }

    public string? Format { get; set; }
    public CultureInfo Culture { get; set; }
}

public class TimePickerControl : IControlRecord
{
    public Control Control { get; set; }

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
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((ComboBox)Control).SelectedItem?.ToString();
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}

public class TextControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((TextBox)Control).Text;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}
public class PasswordControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((PasswordBox)Control).Password;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}
public class FileContent : IControlRecord
{
    public Control Control { get; set; }
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
    public Control Control { get; set; }

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
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((FilePicker)Control).FilePath;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}

public class DirectoryPickerControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((DirectoryPicker)Control).DirPath;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}

public class NumericControl : IControlRecord
{
    public Control Control { get; set; }

    public string GetFormattedValue()
    {
        return ((NumericUpDown)Control).Text;
    }

    public string Name { get; set; }
    public bool MaskingRequired { get; set; }
}

public interface IControlRecord
{
    Control Control { get; set; }

    string GetFormattedValue();

    public string Name { get; set; }

    public bool MaskingRequired { get; set; }
}

public class JobStatusToColorConverter:  IValueConverter
{

    public static JobStatusToColorConverter Instance { get; } = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RunningJobStatus status)
        {
            return status switch
            {
                RunningJobStatus.NotStarted => new SolidColorBrush(Colors.Black),
                RunningJobStatus.Running => new SolidColorBrush(Colors.LightGreen),
                RunningJobStatus.Cancelled => new SolidColorBrush(Colors.Yellow),
                RunningJobStatus.Failed => new SolidColorBrush(Colors.Red),
                RunningJobStatus.Finished => new SolidColorBrush(Colors.Gray),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}