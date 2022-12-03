using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ReactiveUI;

namespace ScriptRunner.GUI.Views
{
    public partial class PredefinedParameterSaveWindow : Window
    {
        public PredefinedParameterSaveWindow()
        {
            InitializeComponent();
        }

        public SavePredefinedParameterVM ViewModel => DataContext as SavePredefinedParameterVM;

        private void Save(object? sender, RoutedEventArgs e)
        {
            var name = ViewModel.UseNew ? ViewModel.NewName : ViewModel.SelectedExisting;
            if (string.IsNullOrWhiteSpace(name) == false)
            {
                Close(name);
            }
            
        }
    }

    public class SavePredefinedParameterVM:ReactiveObject
    {
        private bool _useNew;
        private bool _useExisting;

        public bool UseNew
        {
            get => _useNew;
            set => this.RaiseAndSetIfChanged(ref _useNew,  value);
        }

        public bool UseExisting
        {
            get => _useExisting;
            set => this.RaiseAndSetIfChanged(ref _useExisting, value);
        }

        public string NewName { get; set; }

        public List<string> ExistingSets { get; set; }
        public string SelectedExisting { get; set; }
    }
}
