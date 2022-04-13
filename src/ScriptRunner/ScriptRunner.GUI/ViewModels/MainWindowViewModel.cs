using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Layout;
using ReactiveUI;

namespace ScriptRunner.GUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ObservableCollection<IControl> _controlsCollection;
        public ObservableCollection<IControl> ControlsCollection
        {
            get => _controlsCollection;
            private set => this.RaiseAndSetIfChanged(ref _controlsCollection, value);
        }

        public void AddNewTextBoxCommand()
        {
            ControlsCollection.Add(new TextBox
            {
                Text = "test text"
            });
        }

        public MainWindowViewModel()
        {
            _controlsCollection = new ObservableCollection<IControl>();

            var config = ScriptConfigReader.Load();
            foreach (var action in config.Actions)
            {
                ControlsCollection.Add(new Label{Content = action.Name});
                ControlsCollection.Add(new TextBlock{Text = action.Description});
                ControlsCollection.Add(new TextBlock{Text = action.Command});

                ControlsCollection.Add(new Label{Content = "Parameters: "});

                foreach (var param in action.Params)
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
                            new TextBox
                            {
                                Text = param.Description
                            }
                        }
                    };
                    ControlsCollection.Add(stackPanel);
                }
            }
        }


    }
}
