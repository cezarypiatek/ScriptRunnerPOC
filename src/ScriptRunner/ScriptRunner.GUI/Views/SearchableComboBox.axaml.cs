using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace ScriptRunner.GUI.Views;

  public partial class SearchableComboBox : UserControl
    {
        public static readonly StyledProperty<ObservableCollection<string>> ItemsProperty =
            AvaloniaProperty.Register<ContentControl, ObservableCollection<string>>(nameof(Items));

        public static readonly StyledProperty<string> SelectedItemProperty =
            AvaloniaProperty.Register<ContentControl, string>(nameof(SelectedItem));

        private AutoCompleteBox? _autoCompleteBox;

        public SearchableComboBox()
        {
            Items = new ObservableCollection<string>();
            ItemsProperty.Changed.Subscribe(args =>
            {
                if(args.Sender is SearchableComboBox searchableComboBox)
                {
                    if(searchableComboBox._autoCompleteBox != null)
                    {
                        searchableComboBox._autoCompleteBox.ItemsSource = args.NewValue.Value;
                    }
                }

            });
            SelectedItemProperty.Changed.Subscribe(args =>
            {
                if(args.Sender is SearchableComboBox searchableComboBox)
                {
                    if(searchableComboBox._autoCompleteBox != null)
                    {
                        searchableComboBox._autoCompleteBox.SelectedItem = args.NewValue.Value;
                    }
                }
            });
            this.InitializeComponent();
        }

        public ObservableCollection<string> Items
        {
            get => GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        public string SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set
            {
                if (Items.Contains(value))
                {
                    SetValue(SelectedItemProperty, value);
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _autoCompleteBox = this.FindControl<AutoCompleteBox>("PART_AutoCompleteBox");

            if (_autoCompleteBox != null)
            {
                _autoCompleteBox.ItemsSource = Items;
                _autoCompleteBox.SelectedItem = SelectedItem;
                //_autoCompleteBox.TextChanged += AutoCompleteBox_TextChanged;
                _autoCompleteBox.GotFocus += (sender, args) =>
                {
                    _autoCompleteBox.IsDropDownOpen = true;
                };
                _autoCompleteBox.LostFocus += (sender, args) =>
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if(_autoCompleteBox.SelectedItem == null)
                            {
                                _autoCompleteBox.Text = "";
                            }
                        });
                    });
                };
               
                _autoCompleteBox.SelectionChanged += AutoCompleteBox_SelectionChanged;
            }
        }

        
        private string previousValue = "";
        private void AutoCompleteBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_autoCompleteBox?.SelectedItem is string selected && selected != previousValue)
            {
                SelectedItem = selected;
                previousValue = selected;
            }
        }

        private void ShowAllOptions_Click(object? sender, RoutedEventArgs e)
        {
            ShowAll();
        }

        public void ShowAll()
        {
            if (_autoCompleteBox != null)
            {
                _autoCompleteBox.Text = "";
                _autoCompleteBox.Focus();
                _autoCompleteBox.ItemsSource = new ObservableCollection<string>(Items);
                _autoCompleteBox.IsDropDownOpen = true;
            }
        }
    }