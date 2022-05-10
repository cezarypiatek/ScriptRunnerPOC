using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ScriptRunner.GUI.Views
{
    public partial class ActionPanelControl : UserControl
    {
        // Action name property
        public static readonly DirectProperty<ActionPanelControl, string> ActionNameProperty =
            AvaloniaProperty.RegisterDirect<ActionPanelControl, string>(nameof(ActionName), o => o.ActionName, (o, v) => o.ActionName = v);

        private string _actionName = "<default action name>";
        public string ActionName
        {
            get { return _actionName; }
            set { SetAndRaise(ActionNameProperty, ref _actionName, value); }
        }

        // Action description property
        public static readonly DirectProperty<ActionPanelControl, string> ActionDescriptionProperty =
            AvaloniaProperty.RegisterDirect<ActionPanelControl, string>(nameof(ActionDescription), o => o.ActionDescription, (o, v) => o.ActionDescription = v);

        private string _actionDescription = "<default action description>";
        public string ActionDescription
        {
            get { return _actionDescription; }
            set { SetAndRaise(ActionDescriptionProperty, ref _actionDescription, value); }
        }

        // Action command property
        public static readonly DirectProperty<ActionPanelControl, string> ActionCommandProperty =
            AvaloniaProperty.RegisterDirect<ActionPanelControl, string>(nameof(ActionCommand), o => o.ActionCommand, (o, v) => o.ActionCommand = v);

        private string _actionCommand = "<default action command>";
        public string ActionCommand
        {
            get { return _actionCommand; }
            set { SetAndRaise(ActionCommandProperty, ref _actionCommand, value); }
        }

        public ActionPanelControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
