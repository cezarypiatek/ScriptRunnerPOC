using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Projektanker.Icons.Avalonia;
using ScriptRunner.GUI.Infrastructure;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.ViewModels;
using ScriptRunner.GUI.Views;
using Path = System.IO.Path;

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
            var controlRecord = CreateControlRecord(param, value, i, action, secretBindings, commandExecutor);
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

            var controlForEdit = controlRecord.Control;
            
            if (param.Prompt is PromptType.Multilinetext or PromptType.FileContent)
            {
                bool _isResizing = false;
                Point _lastPointerPosition = default;
                
                var resizeHandle = new Border()
                {
                    Height = 10,
                    Background = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    ZIndex = 1,
                    Margin = new Thickness(0,-10,0,0),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Cursor = new Cursor(StandardCursorType.SizeNorthSouth)
                };
                resizeHandle.Child = new Icon()
                {
                    Value = "fas fa-signal",
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                

                resizeHandle.PointerPressed += (sender, e) =>
                {
                    _isResizing = true;
                    _lastPointerPosition = e.GetPosition(paramsPanel);
                    e.Handled = true;
                };

                resizeHandle.PointerMoved += (sender, e) =>
                {
                    if (_isResizing)
                    {
                        var currentPosition = e.GetPosition(paramsPanel);
                        var delta = currentPosition - _lastPointerPosition;

                        var textBox = controlRecord.Control;
                        textBox.Height = Math.Max(textBox.MinHeight, textBox.Height + delta.Y);

                        _lastPointerPosition = currentPosition;
                    }
                };

                resizeHandle.PointerReleased += (sender, e) =>
                {
                    _isResizing = false;
                };
                //paramsPanel.Children.Add(resizeHandle);

                var panel = new StackPanel()
                {
                    Orientation = Orientation.Vertical
                };
                panel.Children.Add(controlRecord.Control);
                panel.Children.Add(resizeHandle);
                controlForEdit = panel;


            }
          
           
            var actionPanel = new StackPanel
            {
                Children =
                {
                    label,
                    controlForEdit
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
                ToolTip.SetTip(generateButton, string.IsNullOrWhiteSpace(param.ValueGeneratorLabel)? "Auto fill": param.ValueGeneratorLabel);
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
        ScriptConfig scriptConfig, List<VaultBinding> secretBindings,
        Func<string, string, Task<string?>> commandExecutor)
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
                var initialOptions = p.GetPromptSettings("options", out var options) ? options.Split(","):Array.Empty<string>();
                var observableOptions = new ObservableCollection<string>(initialOptions);
                var searchable = p.GetPromptSettings("searchable", bool.Parse, false);
                var optionsGeneratorCommand = p.GetPromptSettings("optionsGeneratorCommand", out var optionsGeneratorCommandText) ? optionsGeneratorCommandText : null;
                
                
                if (observableOptions.Count == 0 && string.IsNullOrWhiteSpace(value) == false && string.IsNullOrWhiteSpace(optionsGeneratorCommand) == false)
                {
                    observableOptions.Add(value);
                }
                
                Control inputControl = searchable ? new SearchableComboBox()
                {
                    Items = observableOptions,
                    SelectedItem = value,
                    TabIndex = index,
                    IsTabStop = true,
                    Width = 500
                }: new ComboBox
                { 
                    ItemsSource = observableOptions,
                    SelectedItem = value,
                    TabIndex = index,
                    IsTabStop = true,
                    Width = 500
                };
                var actionPanel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                    Children =
                    {
                        inputControl
                    }
                };
                if (string.IsNullOrWhiteSpace(optionsGeneratorCommand) == false)
                {
                    var generateButton = new Button()
                    {
                        Margin = new(5,0,5,0),
                        Width = 32,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Center
                    };
                    generateButton.Click += async(sender, args) =>
                    {
                        generateButton.IsEnabled = false;
                        generateButton.Classes.Add("spinning");
                        var result = await commandExecutor($"Generate options for '{p.Name}'", optionsGeneratorCommand) ?? "";
                        Dispatcher.UIThread.Post(() =>
                        {
                            observableOptions.Clear();
                            foreach (var option in result.Split(new[]{'\r', '\n',','}, StringSplitOptions.RemoveEmptyEntries).Distinct().OrderBy(x=>x))
                            {
                                observableOptions.Add(option);
                            }
                            generateButton.Classes.Remove("spinning");
                            generateButton.IsEnabled = true;
                        });
                    };
                    Attached.SetIcon(generateButton, "fas fa-sync");
                    ToolTip.SetTip(generateButton, "Refresh available options");
                    actionPanel.Children.Add(generateButton);
                }
               
                return new DropdownControl
                {
                    Control = actionPanel,
                    InputControl = inputControl
                };
            case PromptType.Multiselect:
                var delimiter = p.GetPromptSettings("delimiter", s => s, ",");
                return new MultiSelectControl
                {
                    Control = new  CheckBoxListBox
                    {
                        SelectionMode = SelectionMode.Multiple,
                        ItemsSource = p.GetPromptSettings("options", out var multiSelectOptions) ? multiSelectOptions.Split(delimiter) : Array.Empty<string>(),
                        SelectedItems = new AvaloniaList<string>((value ?? string.Empty).Split(delimiter)),
                        TabIndex = index,
                        IsTabStop = true,
                        BorderBrush = new SolidColorBrush(Color.Parse("#99ffffff")),
                        CornerRadius = new CornerRadius(3),
                        BorderThickness = new Thickness(1),
                        Width = 500
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
                var (defaultChecked, defaultUnchecked) = scriptConfig.AutoParameterBuilderStyle == "powershell" ? ("$true", "$false") : ("true", "false");
                var checkedValueText  = p.GetPromptSettings("checkedValue", out var checkedValue)? checkedValue: defaultChecked;
                return new CheckboxControl
                {
                    Control = new CheckBox
                    {
                        IsChecked = string.IsNullOrWhiteSpace(value) == false && value == checkedValueText,
                        TabIndex = index,
                        IsTabStop = true
                    },
                    CheckedValue = checkedValueText,
                    UncheckedValue =  p.GetPromptSettings("uncheckedValue", out var uncheckedValue)? uncheckedValue: defaultUnchecked,
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
                        Width = 500,
                        
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