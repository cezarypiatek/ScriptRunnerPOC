using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
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

        public SearchBox(List<ScriptConfig> viewModelActions)
        {
            InitializeComponent();
            ViewModel = new SearchBoxViewModel(viewModelActions);
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
                        FilteredItems.SelectedIndex = FilteredItems.ItemCount -1;

                        if (FilteredItems.ItemContainerGenerator.ContainerFromIndex(FilteredItems.SelectedIndex) is { } item)
                        {
                            item.Focus();
                        }
                    }
                }
                else if (args.Key == Key.Down)
                {
                    if (FilteredItems.ItemCount > 0)
                    {
                        FilteredItems.SelectedIndex = 0;

                        if (FilteredItems.ItemContainerGenerator.ContainerFromIndex(0) is { } item)
                        {
                            item.Focus();
                        }
                    }
                }else if (args.Key == Key.Enter)
                {
                    ChoseSelected();
                }


            };

            this.FilteredItems.KeyUp += (sender, args) =>
            {
                if (args.Key == Key.Up)
                {
                    if (FilteredItems.SelectedIndex == 0)
                    {
                        this.SearchBoxInput.Focus();

                    }
                }
                else if (args.Key == Key.Enter)
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



            this.FilteredItems.KeyDown += (sender, args) =>
            {
                if (args.Key is not Key.Escape or Key.Down or Key.Up or Key.Enter)
                {
                    SearchBoxInput.Focus();
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
    }
}
