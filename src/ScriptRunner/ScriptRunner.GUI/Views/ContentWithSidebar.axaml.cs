using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Search;


namespace ScriptRunner.GUI.Views
{
    public partial class ContentWithSidebar : UserControl
    {
        public ContentWithSidebar()
        {
            InitializeComponent();
            
        }

        static ContentWithSidebar()
        {
            SidebarProperty.Changed.Subscribe(OnSidebarPositionChanged);
            IsSidebarOpenProperty.Changed.Subscribe(OnSidebarPositionChanged);
            SidebarPositionProperty.Changed.Subscribe(OnSidebarPositionChanged);
        }

        private static void OnSidebarPositionChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is ContentWithSidebar control)
            {
                    SetPositions( control);
            }
        }

        
        private static void SetPositions(ContentWithSidebar control)
        {
            SideBarPositions val = control.SidebarPosition;
            if (control.IsSidebarOpen)
            {
                control.SidePanelContainer?.SetValue(IsVisibleProperty, true);
                control.Splitter?.SetValue(IsVisibleProperty, true);
                control.MainPanelContainer?.SetValue(Grid.ColumnSpanProperty, 1);
                
                if (val is SideBarPositions.Right)
                {
                    control.MainPanelContainer?.SetValue(Grid.ColumnProperty, 0);
                    control.SidePanelContainer?.SetValue(Grid.ColumnProperty, 2);
                }
                else
                {
                    control.MainPanelContainer?.SetValue(Grid.ColumnProperty, 2);
                    control.SidePanelContainer?.SetValue(Grid.ColumnProperty, 0);
                }
            }
            else
            {
                control.SidePanelContainer?.SetValue(IsVisibleProperty, false);
                control.Splitter?.SetValue(IsVisibleProperty, false);
                control.MainPanelContainer?.SetValue(Grid.ColumnSpanProperty, 3);
                if (val is SideBarPositions.Left)
                {
                    control.MainPanelContainer?.SetValue(Grid.ColumnProperty, 0);
                }
            }
            
            if (val is SideBarPositions.Right)
            {
                
                if (control.GridPart != null)
                {
                    control.GridPart.ColumnDefinitions[2].Width = control.SidebarWidth.HasValue ? new GridLength(control.SidebarWidth.Value): GridLength.Auto;
                    control.GridPart.ColumnDefinitions[0].Width = GridLength.Star;
                    if (control.MainMinWidth is {} mainMinWidth)
                    {
                        control.GridPart.ColumnDefinitions[0].MinWidth = mainMinWidth;
                    }
                    
                }
            }
            else
            {
             
                if (control.GridPart != null)
                {
                    control.GridPart.ColumnDefinitions[0].Width =  control.SidebarWidth.HasValue ? new GridLength(control.SidebarWidth.Value): GridLength.Auto;
                    control.GridPart.ColumnDefinitions[2].Width = GridLength.Star;
                    if (control.MainMinWidth is {} mainMinWidth)
                    {
                        control.GridPart.ColumnDefinitions[2].MinWidth = mainMinWidth;
                    }
                }
            }
        }


        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            MainPanelContainer = e.NameScope.Find("PART_MainPanel") as Control;
            SidePanelContainer = e.NameScope.Find("PART_SidebarPanel") as Control;
            Splitter = e.NameScope.Find("PART_Splitter") as GridSplitter;
            GridPart = e.NameScope.Find("PART_Grid") as Grid;
            SetPositions(this);
        }

        public GridSplitter? Splitter { get; set; }

        public Grid? GridPart { get; set; }

        public Control? SidePanelContainer { get; set; }

        public Control? MainPanelContainer { get; set; }

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

        public SideBarPositions SidebarPosition
        {
            get { return GetValue(SidebarPositionProperty); }
            set { SetValue(SidebarPositionProperty, value); }
        }

        public static readonly StyledProperty<SideBarPositions> SidebarPositionProperty = AvaloniaProperty.Register<ContentControl, SideBarPositions>(nameof(SidebarPosition));

        public double? SidebarWidth
        {
            get { return GetValue(SidebarWidthProperty); }
            set { SetValue(SidebarWidthProperty, value); }
        }

        public static readonly StyledProperty<double?> SidebarWidthProperty = AvaloniaProperty.Register<ContentControl, double?>(nameof(SidebarWidth));
        
        public double? MainMinWidth
        {
            get { return GetValue(MainMinWidthProperty); }
            set { SetValue(MainMinWidthProperty, value); }
        }

        public static readonly StyledProperty<double?> MainMinWidthProperty = AvaloniaProperty.Register<ContentControl, double?>(nameof(MainMinWidth));
    }

    public enum SideBarPositions
    {
        Right,
        Left
    }
    
    public class StringMatchConverter : IValueConverter
    {
        public static readonly StringMatchConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var parameterString = parameter?.ToString() ?? "null";
            var valueString = value?.ToString() ?? "null";
            return parameterString.Equals(valueString, StringComparison.InvariantCultureIgnoreCase);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
