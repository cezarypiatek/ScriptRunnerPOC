using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ScriptRunner.GUI.Views;

public partial class RepositoryChangesWindow : Window
{
    public RepositoryChangesWindow()
        : this(Array.Empty<string>())
    {
    }

    public RepositoryChangesWindow(IEnumerable<string> changes)
    {
        Changes = changes
            .Where(change => !string.IsNullOrWhiteSpace(change))
            .Select(change => change.Trim())
            .ToArray();
        Summary = Changes.Count == 1
            ? "1 new change was downloaded"
            : $"{Changes.Count} new changes were downloaded";

        InitializeComponent();
        DataContext = this;
    }

    public IReadOnlyList<string> Changes { get; }

    public string Summary { get; }

    private void CloseWindow(object? sender, RoutedEventArgs e) => Close();
}
