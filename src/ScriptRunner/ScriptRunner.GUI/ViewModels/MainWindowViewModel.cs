using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using DynamicData;
using ReactiveUI;
using ScriptRunner.GUI.ScriptReader;
using Console = System.Console;

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

        private ObservableCollection<string> _actions;
        public ObservableCollection<string> Actions
        {
            get => _actions;
            private set => this.RaiseAndSetIfChanged(ref _actions, value);
        }

        public MainWindowViewModel()
        {
            _controlsCollection = new ObservableCollection<IControl>();
            _actions = new ObservableCollection<string>();

            BuildUi();
        }
        
        private IObservable<string> _textObserver;

        private void BuildUi()
        {
            var config = ScriptConfigReader.Load();
            foreach (var action in config.Actions)
            {
                Actions.Add(action.Name);

                // creating field
                var testTextBox = new TextBox();
                _textObserver = testTextBox.GetObservable(TextBox.TextProperty);
                var disposableToDispose = _textObserver.Subscribe();
                ControlsCollection.Add(testTextBox);



                ControlsCollection.AddRange(UiFactory.BuildControls(action));
            }
        }

        public void RunScript()
        {
            var wartosc = _textObserver.FirstAsync().GetAwaiter().GetResult();

        }
    }
}
