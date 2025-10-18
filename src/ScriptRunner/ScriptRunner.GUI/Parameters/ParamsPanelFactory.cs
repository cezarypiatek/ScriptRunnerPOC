using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.TextMate;
using Projektanker.Icons.Avalonia;
using ScriptRunner.GUI.Infrastructure;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.Settings;
using ScriptRunner.GUI.ViewModels;
using ScriptRunner.GUI.Views;
using TextMateSharp.Grammars;
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
                // Don't set MaxWidth for multiline/file content controls since they have resize handles
                if (param.Prompt is not (PromptType.Multilinetext or PromptType.FileContent))
                {
                    l.MaxWidth = 500;
                }
            }

            var label = new Label
            {
                Content = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(param.Description) ? param.Name : param.Description,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 300
                }
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
                    Width = 10,
                    Background = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    ZIndex = 1,
                    Margin = new Thickness(0,-10,0,0),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Cursor = new Cursor(StandardCursorType.BottomRightCorner)
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
                        
                        // Resize vertically
                        textBox.Height = Math.Max(textBox.MinHeight, textBox.Height + delta.Y);
                        
                        // Resize horizontally
                        var currentWidth = double.IsNaN(textBox.Width) ? textBox.Bounds.Width : textBox.Width;
                        textBox.Width = Math.Max(100, currentWidth + delta.X);

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
                        }
                        else if (controlRecord is { Control: TextEditor te })
                        {
                            te.Text = result?.Trim() ?? string.Empty;
                        }
                        generateButton.Classes.Remove("spinning");
                        generateButton.IsEnabled = true;
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
                var delimiterForOptions = p.GetPromptSettings("delimiter", x => x, ",");
                var dropdownOptions = p.GetDropdownOptions(delimiterForOptions);
                var observableDropdownOptions = new ObservableCollection<DropdownOption>(dropdownOptions);
                var searchable = p.GetPromptSettings("searchable", bool.Parse, false);
                var optionsGeneratorCommand = p.GetPromptSettings("optionsGeneratorCommand", out var optionsGeneratorCommandText) ? optionsGeneratorCommandText : null;
                
                // Find selected item by matching value
                DropdownOption? selectedOption = null;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    selectedOption = observableDropdownOptions.FirstOrDefault(opt => opt.Value == value);
                    if (selectedOption == null && string.IsNullOrWhiteSpace(optionsGeneratorCommand) == false)
                    {
                        // Add the value as a temporary option if not found and generator is available
                        selectedOption = new DropdownOption(value);
                        observableDropdownOptions.Add(selectedOption);
                    }
                }
                
                Control inputControl;
                
                if (searchable)
                {
                    // For searchable, convert to strings (SearchableComboBox only supports strings)
                    var stringOptions = new ObservableCollection<string>(dropdownOptions.Select(o => o.Label));
                    var selectedString = selectedOption?.Label;
                    
                    var searchBox = new SearchableComboBox()
                    {
                        Items = stringOptions,
                        TabIndex = index,
                        IsTabStop = true,
                        Width = 500
                    };
                    
                    // Set selected item after Items collection is set
                    if (!string.IsNullOrWhiteSpace(selectedString) && stringOptions.Contains(selectedString))
                    {
                        searchBox.SelectedItem = selectedString;
                    }
                    
                    inputControl = searchBox;
                }
                else
                {
                    inputControl = new ComboBox
                    { 
                        ItemsSource = observableDropdownOptions,
                        SelectedItem = selectedOption,
                        TabIndex = index,
                        IsTabStop = true,
                        Width = 500
                    };
                }
                
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
                    bool wasGenerated = false;
                    EventHandler<RoutedEventArgs> generate = async(sender, args) =>
                    {
                        generateButton.IsEnabled = false;
                        generateButton.Classes.Add("spinning");
                        var result = await commandExecutor($"Generate options for '{p.Name}'", optionsGeneratorCommand) ?? "";
                        Dispatcher.UIThread.Post(() =>
                        {
                            var newOptions = result.Split(new[]{"\r", "\n",delimiterForOptions}, StringSplitOptions.RemoveEmptyEntries)
                                .Distinct()
                                .OrderBy(x=>x)
                                .Select(opt => new DropdownOption(opt.Trim()))
                                .ToList();
                                
                            if (searchable && inputControl is SearchableComboBox searchBox)
                            {
                                searchBox.Items.Clear();
                                foreach (var option in newOptions)
                                {
                                    searchBox.Items.Add(option.Label);
                                }
                            }
                            else if (inputControl is ComboBox comboBox)
                            {
                                observableDropdownOptions.Clear();
                                foreach (var option in newOptions)
                                {
                                    observableDropdownOptions.Add(option);
                                }
                            }
                            
                            generateButton.Classes.Remove("spinning");
                            generateButton.IsEnabled = true;
                            wasGenerated = true;
                            if (inputControl is SearchableComboBox scb2)
                            {
                                scb2.ShowAll();
                            }
                        });
                    };
                    generateButton.Click += generate;
                    if(inputControl is SearchableComboBox searchableBox)
                    {
                        searchableBox.GotFocus += (sender, args) =>
                        {
                            if (wasGenerated == false)
                            {
                                generate(sender, args);
                            }
                        };
                    }
                    Attached.SetIcon(generateButton, "fas fa-sync");
                    ToolTip.SetTip(generateButton, "Refresh available options");
                    actionPanel.Children.Add(generateButton);
                }
               
                return new DropdownControl
                {
                    Control = actionPanel,
                    InputControl = inputControl,
                    DropdownOptions = observableDropdownOptions
                };
            case PromptType.Multiselect:
                var delimiter = p.GetPromptSettings("delimiter", s => s, ",");
                var multiSelectOptions = p.GetDropdownOptions(delimiter);
                
                // Parse selected values
                var selectedValues = (value ?? string.Empty).Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim()).ToList();
                var selectedDropdownOptions = multiSelectOptions.Where(opt => selectedValues.Contains(opt.Value)).ToList();
                
                return new MultiSelectControl
                {
                    Control = new  CheckBoxListBox
                    {
                        SelectionMode = SelectionMode.Multiple,
                        ItemsSource = multiSelectOptions,
                        SelectedItems = new AvaloniaList<DropdownOption>(selectedDropdownOptions),
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
                if (p.GetPromptSettings("syntax", out var syntax) && !string.IsNullOrWhiteSpace(syntax))
                {
                    return new TextControl
                    {
                        Control = CreateAvaloniaEdit(value, index, syntax)
                    };
                }

                // Use regular TextBox without syntax highlighting
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

                var fileExtension = p.GetPromptSettings("extension", out var extension)?extension:"dat";
                return new FileContent(fileExtension)
                {
                    Control = CreateAvaloniaEdit(textForControl, index, fileExtension.TrimStart('.'))
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

    private static TextEditor CreateAvaloniaEdit(string? value, int index, string syntax)
    {
        var textEditor = new TextEditor
        {
            Document = new TextDocument(value ?? string.Empty),
            TabIndex = index,
            MinHeight = 100,
            Height = 100,
            Width = 500,
            ShowLineNumbers = true,
            FontFamily = new FontFamily("Cascadia Code,Consolas,Menlo,Monospace"),
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(153, 255, 255,255)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3)
        };
        textEditor.TextArea.TextView.Margin = new Thickness(10, 0);
        var registry  = new  RegistryOptions(ThemeName.DarkPlus);
        TextMate.Installation textMateInstallation = textEditor.InstallTextMate(registry);
        if (registry.GetLanguageByExtension("." + syntax) is { } languageByExtension)
        {
            textMateInstallation.SetGrammar(registry.GetScopeByLanguageId(languageByExtension.Id));
        }

        return textEditor;
    }
}
