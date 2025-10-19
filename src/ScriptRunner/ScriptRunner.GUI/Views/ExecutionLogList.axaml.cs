using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ScriptRunner.GUI.ViewModels;

namespace ScriptRunner.GUI.Views;

public partial class ExecutionLogList : UserControl
{
    public static readonly StyledProperty<IEnumerable<ExecutionLogAction>?> ItemsProperty =
        AvaloniaProperty.Register<ExecutionLogList, IEnumerable<ExecutionLogAction>?>(nameof(Items));

    public static readonly StyledProperty<IEnumerable<ExecutionLogItemBase>?> GroupedItemsProperty =
        AvaloniaProperty.Register<ExecutionLogList, IEnumerable<ExecutionLogItemBase>?>(nameof(GroupedItems));

    public static readonly StyledProperty<ExecutionLogAction?> SelectedItemProperty =
        AvaloniaProperty.Register<ExecutionLogList, ExecutionLogAction?>(nameof(SelectedItem), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<ExecutionLogItemBase?> SelectedLogItemProperty =
        AvaloniaProperty.Register<ExecutionLogList, ExecutionLogItemBase?>(nameof(SelectedLogItem), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> ShowDatePickerProperty =
        AvaloniaProperty.Register<ExecutionLogList, bool>(nameof(ShowDatePicker), defaultValue: false);

    public static readonly StyledProperty<bool> IsDatePickerVisibleProperty =
        AvaloniaProperty.Register<ExecutionLogList, bool>(nameof(IsDatePickerVisible), defaultValue: false);

    public static readonly StyledProperty<IEnumerable<DateGroupInfo>?> AvailableDatesProperty =
        AvaloniaProperty.Register<ExecutionLogList, IEnumerable<DateGroupInfo>?>(nameof(AvailableDates));

    private INotifyCollectionChanged? _currentCollection;

    public IEnumerable<ExecutionLogAction>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public IEnumerable<ExecutionLogItemBase>? GroupedItems
    {
        get => GetValue(GroupedItemsProperty);
        private set => SetValue(GroupedItemsProperty, value);
    }

    public ExecutionLogAction? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public ExecutionLogItemBase? SelectedLogItem
    {
        get => GetValue(SelectedLogItemProperty);
        set => SetValue(SelectedLogItemProperty, value);
    }

    public bool ShowDatePicker
    {
        get => GetValue(ShowDatePickerProperty);
        set => SetValue(ShowDatePickerProperty, value);
    }

    public bool IsDatePickerVisible
    {
        get => GetValue(IsDatePickerVisibleProperty);
        set => SetValue(IsDatePickerVisibleProperty, value);
    }

    public IEnumerable<DateGroupInfo>? AvailableDates
    {
        get => GetValue(AvailableDatesProperty);
        private set => SetValue(AvailableDatesProperty, value);
    }

    public ExecutionLogList()
    {
        InitializeComponent();
        
        // Watch for changes to Items and rebuild grouped list
        this.GetObservable(ItemsProperty).Subscribe(items =>
        {
            // Unsubscribe from old collection
            if (_currentCollection != null)
            {
                _currentCollection.CollectionChanged -= OnCollectionChanged;
            }
            
            // Subscribe to new collection if it's observable
            if (items is INotifyCollectionChanged observable)
            {
                _currentCollection = observable;
                observable.CollectionChanged += OnCollectionChanged;
            }
            else
            {
                _currentCollection = null;
            }
            
            RebuildGroupedList();
            RebuildAvailableDates();
        });
        
        // Watch for changes to SelectedLogItem and update SelectedItem
        this.GetObservable(SelectedLogItemProperty).Subscribe(item =>
        {
            if (item is ExecutionLogDateHeader)
            {
                // Ignore date header selections
                SelectedLogItem = null;
                return;
            }
            
            if (item is ExecutionLogItemAction actionItem)
            {
                SelectedItem = actionItem.Action;
            }
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void RebuildGroupedList()
    {
        if (Items == null)
        {
            GroupedItems = null;
            return;
        }

        var items = new List<ExecutionLogItemBase>();
        DateTime? lastDate = null;
        
        foreach (var action in Items)
        {
            var actionDate = action.Timestamp.Date;
            
            // Add date header if the date changed
            if (lastDate == null || lastDate != actionDate)
            {
                items.Add(new ExecutionLogDateHeader(actionDate));
                lastDate = actionDate;
            }
            
            items.Add(new ExecutionLogItemAction(action));
        }
        
        GroupedItems = items;
    }

    private void RebuildAvailableDates()
    {
        if (Items == null)
        {
            AvailableDates = null;
            return;
        }

        var dateGroups = Items
            .GroupBy(a => a.Timestamp.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new DateGroupInfo(g.Key, g.Count()))
            .ToList();

        AvailableDates = dateGroups;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildGroupedList();
        RebuildAvailableDates();
    }

    public void OnDateHeaderClicked(object? sender, PointerPressedEventArgs e)
    {
        // Show the date picker overlay when a date header is clicked (if enabled)
        if (ShowDatePicker)
        {
            IsDatePickerVisible = true;
        }
        e.Handled = true;
    }

    public void OnDatePickerOverlayClicked(object? sender, PointerPressedEventArgs e)
    {
        // Close the overlay when clicking on the background
        IsDatePickerVisible = false;
    }

    public void OnDatePickerItemClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.DataContext is DateGroupInfo dateInfo)
        {
            IsDatePickerVisible = false;
            _ = ScrollToDate(dateInfo.Date);
        }
        e.Handled = true;
    }

    public async Task ScrollToDate(DateTime date)
    {
        var listBox = this.FindControl<ListBox>("ExecutionLogListBox");
        if (listBox == null || GroupedItems == null) return;

        var items = GroupedItems.ToList();
        var targetItem = items.FirstOrDefault(item => 
            item is ExecutionLogDateHeader header && header.Date == date.Date);

        if (targetItem != null)
        {
            listBox.ScrollIntoView(targetItem);
            
            await Task.Delay(200);
            
            var itemContainer = listBox.ContainerFromItem(targetItem);
            if (itemContainer != null)
            {
                var border = itemContainer.GetVisualDescendants()
                    .OfType<Border>()
                    .FirstOrDefault(b => b.Name == "DateHeaderBorder");
                
                if (border != null)
                {
                    border.Classes.Add("highlight");
                    await Task.Delay(2000);
                    border.Classes.Remove("highlight");
                }
            }
        }
    }
}
