using Avalonia;
using Avalonia.Controls;

namespace ScriptRunner.GUI.Views
{
    public partial class ContentWithSidebar : UserControl
    {
        public ContentWithSidebar()
        {
            InitializeComponent();
        }


        public object? Main
        {
            get { return GetValue(MainProperty); }
            set { SetValue(MainProperty, value); }
        }

        public static readonly StyledProperty<object?> MainProperty = AvaloniaProperty.Register<ContentControl, object?>(nameof(Main));
        
        public object? Sidebar
        {
            get { return GetValue(SidebarProperty); }
            set { SetValue(SidebarProperty, value); }
        }

        public static readonly StyledProperty<object?> SidebarProperty = AvaloniaProperty.Register<ContentControl, object?>(nameof(Sidebar));

        public bool IsSidebarOpen
        {
            get { return GetValue(IsSidebarOpenProperty); }
            set { SetValue(IsSidebarOpenProperty, value); }
        }

        public static readonly StyledProperty<bool> IsSidebarOpenProperty = AvaloniaProperty.Register<ContentControl, bool>(nameof(IsSidebarOpen));

    }
}
