using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using ReactiveUI;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views
{
    public partial class SearchBox :  ReactiveUserControl<SearchBoxViewModel>
    {
        public SearchBox():this(Array.Empty<ScriptConfig>(), Array.Empty<RecentAction>())
        {
        }

        public SearchBox(IReadOnlyList<ScriptConfig> viewModelActions, IReadOnlyList<RecentAction> recentActions)
        {
            InitializeComponent();
            ViewModel = new SearchBoxViewModel(viewModelActions, recentActions);
            
            this.WhenActivated(disposables => 
            {
                this.SearchBoxInput.Focus();
            });

            this.SearchBoxInput.KeyDown += (sender, args) =>
            { 
                if (args.Key == Key.Up)
                {
                    if (FilteredItems.ItemCount > 0)
                    {

                        if (FilteredItems.SelectedIndex == 0)
                        {
                            FilteredItems.SelectedIndex = FilteredItems.ItemCount -1;    
                        }
                        else
                        {
                            FilteredItems.SelectedIndex--;
                        }
                    }
                }
                else if (args.Key == Key.Down)
                {
                    if (FilteredItems.ItemCount > 0)
                    {

                        if (FilteredItems.SelectedIndex == FilteredItems.ItemCount-1)
                        {
                            FilteredItems.SelectedIndex = 0;    
                        }
                        else
                        {
                            FilteredItems.SelectedIndex++;
                        }
                    }
                }
                else if (args.Key == Key.Enter)
                {
                    var autoLaunch = args.KeyModifiers.HasFlag(KeyModifiers.Control);
                    ChoseSelected(autoLaunch);
                }
            };

           
            this.FilteredItems.KeyUp += (sender, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    var autoLaunch = args.KeyModifiers.HasFlag(KeyModifiers.Control);
                    ChoseSelected(autoLaunch);
                }


            };

            this.ViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(ViewModel.FilteredActionList))
                {
                    if (FilteredItems.ItemCount > 0)
                    {
                        FilteredItems.SelectedIndex = 0;
                    }
                }
            };
        }

        private void ChoseSelected(bool autoLaunch = false)
        {
            if (FilteredItems.SelectedItem is ScriptConfigWithArgumentSet selectedConfig)
            {
                OnResultSelected(selectedConfig, autoLaunch);
            }
        }

        private void ClickOnItem(object? sender, PointerReleasedEventArgs e)
        {
            if (FilteredItems.SelectedItem is ScriptConfigWithArgumentSet selectedConfig)
            {
                var autoLaunch = e.KeyModifiers.HasFlag(KeyModifiers.Control);
                OnResultSelected(selectedConfig, autoLaunch);
            }
        }

        public event EventHandler<ResultSelectedEventArgs> ResultSelected;

        public class ResultSelectedEventArgs : EventArgs
        {
            public ScriptConfigWithArgumentSet? Result { get; set; }
            public bool AutoLaunch { get; set; }
            public ResultSelectedEventArgs()
            {
            }
        }

        private void OnResultSelected(ScriptConfigWithArgumentSet? v, bool autoLaunch) => ResultSelected?.Invoke(this, new ResultSelectedEventArgs(){Result = v, AutoLaunch = autoLaunch});
    }
}
