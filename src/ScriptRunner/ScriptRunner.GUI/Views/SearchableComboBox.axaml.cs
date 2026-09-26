using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ScriptRunner.GUI.Views;

public partial class SearchableComboBox : UserControl
{
    public static readonly StyledProperty<IList> ItemsProperty =
        AvaloniaProperty.Register<SearchableComboBox, IList>(nameof(Items));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<SearchableComboBox, object?>(nameof(SelectedItem));

    public static readonly StyledProperty<string?> DisplayMemberPathProperty =
        AvaloniaProperty.Register<SearchableComboBox, string?>(nameof(DisplayMemberPath));

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<SearchableComboBox, string?>(nameof(Watermark), "Select an option...");

    private Button? _surfaceButton;
    private TextBlock? _selectedText;
    private Projektanker.Icons.Avalonia.Icon? _chevron;
    private Popup? _popup;
    private Border? _popupBorder;
    private TextBox? _queryBox;
    private ItemsControl? _resultsItems;
    private StackPanel? _emptyState;
    private Border? _resultFooter;
    private TextBlock? _resultCount;
    private INotifyCollectionChanged? _observedCollection;
    private List<object> _filteredItems = new();
    private int _highlightedIndex = -1;
    private int _pendingSelectionVersion;
    private bool _updatingQuery;
    private bool _wasOpenOnPointerPress;
    private bool _initialized;

    public SearchableComboBox()
    {
        Items = new ObservableCollection<object>();
        InitializeComponent();
    }

    public IList Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string? DisplayMemberPath
    {
        get => GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsProperty)
        {
            ObserveItemsCollection();
            CancelPendingSelection();

            // A replacement collection usually means the owning action changed.
            // Close any stale popup/query and load the new action's options.
            if (_initialized)
            {
                CloseDropdown();
                ClearQuery();
                RefreshResults(string.Empty, preferSelectedItem: true);
            }
        }
        else if (change.Property == SelectedItemProperty)
        {
            UpdateClosedPresentation();
            if (_popup?.IsOpen == true)
            {
                RefreshResults(_queryBox?.Text, preferSelectedItem: false);
            }
        }
        else if (change.Property == DisplayMemberPathProperty)
        {
            UpdateClosedPresentation();
            ConfigureItemTemplate();
            RefreshResults(_queryBox?.Text, preferSelectedItem: true);
        }
        else if (change.Property == WatermarkProperty)
        {
            UpdateClosedPresentation();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _surfaceButton = this.FindControl<Button>("PART_SurfaceButton");
        _selectedText = this.FindControl<TextBlock>("PART_SelectedText");
        _chevron = this.FindControl<Projektanker.Icons.Avalonia.Icon>("PART_Chevron");
        _popup = this.FindControl<Popup>("PART_Popup");
        _popupBorder = this.FindControl<Border>("PART_PopupBorder");
        _queryBox = this.FindControl<TextBox>("PART_QueryBox");
        _resultsItems = this.FindControl<ItemsControl>("PART_ResultsItems");
        _emptyState = this.FindControl<StackPanel>("PART_EmptyState");
        _resultFooter = this.FindControl<Border>("PART_ResultFooter");
        _resultCount = this.FindControl<TextBlock>("PART_ResultCount");

        if (_popup != null)
        {
            _popup.PlacementTarget = this;
            _popup.Opened += Popup_Opened;
            _popup.Closed += Popup_Closed;
        }

        ObserveItemsCollection();
        ConfigureItemTemplate();
        UpdateClosedPresentation();
        RefreshResults(string.Empty, preferSelectedItem: true);
        _initialized = true;
    }

    private void ObserveItemsCollection()
    {
        if (_observedCollection != null)
        {
            _observedCollection.CollectionChanged -= Items_CollectionChanged;
        }

        _observedCollection = Items as INotifyCollectionChanged;
        if (_observedCollection != null)
        {
            _observedCollection.CollectionChanged += Items_CollectionChanged;
        }
    }

    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshResults(_popup?.IsOpen == true ? _queryBox?.Text : string.Empty, preferSelectedItem: true);
        UpdateClosedPresentation();
    }

    private void ConfigureItemTemplate()
    {
        if (_resultsItems == null)
        {
            return;
        }

        _resultsItems.ItemTemplate = new FuncDataTemplate<object>(CreateResultButton, supportsRecycling: false);
    }

    private Control CreateResultButton(object item, Avalonia.Controls.INameScope _)
    {
        var label = new TextBlock
        {
            Text = GetDisplayText(item),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        var checkmark = new TextBlock
        {
            Text = "✓",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.LightBlue,
            Margin = new Thickness(12, 0, 2, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            IsVisible = Equals(item, SelectedItem)
        };

        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        content.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        content.Children.Add(label);
        Grid.SetColumn(checkmark, 1);
        content.Children.Add(checkmark);

        var button = new Button
        {
            Content = content,
            Tag = item,
            IsTabStop = false,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
        };
        button.Classes.Add("resultItem");
        button.Click += ResultButton_Click;
        button.PointerEntered += ResultButton_PointerEntered;

        if (Equals(item, SelectedItem))
        {
            button.Classes.Add("current");
        }

        return button;
    }

    private string GetDisplayText(object? item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(DisplayMemberPath))
        {
            return item.ToString() ?? string.Empty;
        }

        PropertyInfo? property = item.GetType().GetProperty(DisplayMemberPath);
        return property?.GetValue(item)?.ToString() ?? string.Empty;
    }

    private void UpdateClosedPresentation()
    {
        if (_selectedText == null)
        {
            return;
        }

        string selectedValue = GetDisplayText(SelectedItem);
        bool hasSelection = !string.IsNullOrWhiteSpace(selectedValue);
        _selectedText.Text = hasSelection ? selectedValue : Watermark;
        _selectedText.Foreground = hasSelection
            ? new SolidColorBrush(Color.Parse("#E2E2E5"))
            : new SolidColorBrush(Color.Parse("#85858D"));
    }

    private void RefreshResults(string? query, bool preferSelectedItem)
    {
        string[] tokens = (query ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        _filteredItems = Items
            .Cast<object?>()
            .Where(item => item != null)
            .Cast<object>()
            .Where(item => tokens.All(token =>
                GetDisplayText(item).Contains(token, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (preferSelectedItem && SelectedItem != null)
        {
            _highlightedIndex = _filteredItems.FindIndex(item => Equals(item, SelectedItem));
        }
        else
        {
            _highlightedIndex = _filteredItems.Count > 0 ? 0 : -1;
        }

        if (_resultsItems != null)
        {
            _resultsItems.ItemsSource = _filteredItems;
        }

        if (_emptyState != null)
        {
            _emptyState.IsVisible = _filteredItems.Count == 0;
        }

        bool queryIsActive = tokens.Length > 0;
        if (_resultFooter != null)
        {
            _resultFooter.IsVisible = Items.Count > 10 || queryIsActive;
        }

        if (_resultCount != null)
        {
            _resultCount.Text = queryIsActive
                ? $"{_filteredItems.Count} of {Items.Count} options"
                : $"{Items.Count} options";
        }

        Dispatcher.UIThread.Post(UpdateResultButtonStates, DispatcherPriority.Loaded);
    }

    private void UpdateResultButtonStates()
    {
        if (_resultsItems == null)
        {
            return;
        }

        object? highlightedItem = _highlightedIndex >= 0 && _highlightedIndex < _filteredItems.Count
            ? _filteredItems[_highlightedIndex]
            : null;

        Button? highlightedButton = null;
        foreach (Button button in _resultsItems.GetVisualDescendants().OfType<Button>()
                     .Where(button => button.Classes.Contains("resultItem")))
        {
            bool isHighlighted = highlightedItem != null && Equals(button.Tag, highlightedItem);
            button.Classes.Set("highlighted", isHighlighted);
            if (isHighlighted)
            {
                highlightedButton = button;
            }
        }

        highlightedButton?.BringIntoView();
    }

    private void SurfaceButton_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _wasOpenOnPointerPress = _popup?.IsOpen == true;
    }

    private void SurfaceButton_Click(object? sender, RoutedEventArgs e)
    {
        bool shouldClose = _wasOpenOnPointerPress || _popup?.IsOpen == true;
        _wasOpenOnPointerPress = false;

        if (shouldClose)
        {
            CloseDropdown();
        }
        else
        {
            OpenDropdown();
        }
    }

    private void SurfaceButton_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F4 || (e.Key == Key.Down && e.KeyModifiers.HasFlag(KeyModifiers.Alt)))
        {
            if (_popup?.IsOpen == true)
            {
                CloseDropdown();
            }
            else
            {
                OpenDropdown();
            }

            e.Handled = true;
        }
    }

    private void SurfaceButton_TextInput(object? sender, TextInputEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Text) && _popup?.IsOpen != true)
        {
            OpenDropdown(e.Text);
            e.Handled = true;
        }
    }

    private void QueryBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!_updatingQuery)
        {
            RefreshResults(_queryBox?.Text, preferSelectedItem: false);
        }
    }

    private void QueryBox_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                MoveHighlight(1);
                e.Handled = true;
                break;
            case Key.Up:
                MoveHighlight(-1);
                e.Handled = true;
                break;
            case Key.Enter:
                if (_highlightedIndex >= 0 && _highlightedIndex < _filteredItems.Count)
                {
                    QueueSelection(_filteredItems[_highlightedIndex]);
                    e.Handled = true;
                }
                break;
            case Key.Escape:
                CloseDropdown();
                _surfaceButton?.Focus();
                e.Handled = true;
                break;
        }
    }

    private void MoveHighlight(int direction)
    {
        if (_filteredItems.Count == 0)
        {
            return;
        }

        _highlightedIndex = _highlightedIndex < 0
            ? (direction > 0 ? 0 : _filteredItems.Count - 1)
            : Math.Clamp(_highlightedIndex + direction, 0, _filteredItems.Count - 1);

        UpdateResultButtonStates();
    }

    private void ResultButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: { } item })
        {
            QueueSelection(item);
        }
    }

    private void ResultButton_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Button { Tag: { } item })
        {
            int index = _filteredItems.FindIndex(candidate => Equals(candidate, item));
            if (index >= 0 && index != _highlightedIndex)
            {
                _highlightedIndex = index;
                UpdateResultButtonStates();
            }
        }
    }

    private void QueueSelection(object item)
    {
        if (Equals(item, SelectedItem))
        {
            CloseDropdown();
            _surfaceButton?.Focus();
            return;
        }

        int requestVersion = ++_pendingSelectionVersion;
        IList sourceItems = Items;
        CloseDropdown();

        // Keep model updates out of the popup button/key event. The selected
        // preset rebuilds the parameter form synchronously.
        Dispatcher.UIThread.Post(() =>
        {
            if (requestVersion == _pendingSelectionVersion &&
                ReferenceEquals(sourceItems, Items) &&
                Items.Contains(item))
            {
                SetCurrentValue(SelectedItemProperty, item);
                _surfaceButton?.Focus();
            }
        }, DispatcherPriority.Background);
    }

    private void CancelPendingSelection()
    {
        _pendingSelectionVersion++;
    }

    private void OpenDropdown(string? initialQuery = null)
    {
        if (_popup == null)
        {
            return;
        }

        SetQuery(initialQuery ?? string.Empty);
        RefreshResults(initialQuery, preferSelectedItem: string.IsNullOrEmpty(initialQuery));

        if (_popupBorder != null)
        {
            _popupBorder.Width = Math.Max(Bounds.Width, 240);
        }

        _popup.IsOpen = true;
    }

    private void CloseDropdown()
    {
        if (_popup != null)
        {
            _popup.IsOpen = false;
        }
    }

    private void SetQuery(string text)
    {
        if (_queryBox == null)
        {
            return;
        }

        _updatingQuery = true;
        _queryBox.Text = text;
        _queryBox.CaretIndex = text.Length;
        _updatingQuery = false;
    }

    private void ClearQuery()
    {
        SetQuery(string.Empty);
    }

    private void Popup_Opened(object? sender, EventArgs e)
    {
        _surfaceButton?.Classes.Add("open");
        if (_chevron != null)
        {
            _chevron.Value = "fas fa-chevron-up";
        }

        Dispatcher.UIThread.Post(() =>
        {
            _queryBox?.Focus();
            UpdateResultButtonStates();
        }, DispatcherPriority.Input);
    }

    private void Popup_Closed(object? sender, EventArgs e)
    {
        _surfaceButton?.Classes.Remove("open");
        if (_chevron != null)
        {
            _chevron.Value = "fas fa-chevron-down";
        }

        ClearQuery();
    }

    public void ShowAll()
    {
        OpenDropdown();
    }
}
