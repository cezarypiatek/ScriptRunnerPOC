using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Controls;
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
            _controlsCollection = new ObservableCollection<IControl>
            {
                new TextBox
                {
                    Text = "test text"
                }
            };
        }


    }
}
