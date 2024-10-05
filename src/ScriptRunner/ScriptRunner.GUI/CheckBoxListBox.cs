using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Styling;

namespace ScriptRunner.GUI;

public class CheckBoxListBox : ListBox
{
    protected override Type StyleKeyOverride { get; } =  typeof(ListBox);
    public CheckBoxListBox()
    {
        this.ItemTemplate = new FuncDataTemplate<object>((item, scope) =>
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            // Create CheckBox and bind to the ListBoxItem's IsSelected property
            var checkBox = new CheckBox
            {
                [!CheckBox.IsCheckedProperty] = new Binding
                {
                    Path = "IsSelected",
                    Mode = BindingMode.TwoWay,
                    RelativeSource = new RelativeSource()
                    {
                        AncestorType = typeof(ListBoxItem),
                        Mode = RelativeSourceMode.FindAncestor
                    }
                }
            };

            // Display the item's content (assuming it's a simple string for demonstration)
            var textBlock = new TextBlock
            {
                // Bind the TextBlock to the item itself
                [!TextBlock.TextProperty] = new Binding("."),
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(checkBox);
            stackPanel.Children.Add(textBlock);

            return stackPanel;
        });

        var style = new Style(x => x.OfType<ListBoxItem>())
        {
            Setters =
            {
                new Setter(ContentPresenter.PaddingProperty, new Thickness(10,5)) // Adjust the padding value
            }
        };
        this.Styles.Add(style);
    }
}
