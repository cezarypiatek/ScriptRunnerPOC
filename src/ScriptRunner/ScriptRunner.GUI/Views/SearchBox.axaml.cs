using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using ReactiveUI;
using ScriptRunner.GUI.ScriptConfigs;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views
{
    public partial class SearchBox :   ReactiveWindow<SearchBoxViewModel>
    {
        public SearchBox()
        {
        }

        public SearchBox(IReadOnlyList<ScriptConfig> viewModelActions, IReadOnlyList<RecentAction> recentActions)
        {
            InitializeComponent();
            ViewModel = new SearchBoxViewModel(viewModelActions, recentActions);
            this.Activated += (sender, args) =>
            {
                this.SearchBoxInput.Focus();
            };
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
                }else if (args.Key == Key.Enter)
                {
                    ChoseSelected();
                }
            };

           
            this.FilteredItems.KeyUp += (sender, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    ChoseSelected();
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

        private void ChoseSelected()
        {
            if (FilteredItems.SelectedItem is ScriptConfigWithArgumentSet selectedConfig)
            {
                Close(selectedConfig);
            }
        }

        private void ClickOnItem(object? sender, PointerReleasedEventArgs e)
        {
            if (FilteredItems.SelectedItem is ScriptConfigWithArgumentSet selectedConfig)
            {
                Close(selectedConfig);
            }
        }

        private void CloseWindow(object? sender, RoutedEventArgs e) => Close();
    }
}
