using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace ScriptRunner.GUI.Views;

public partial class TextEditorOverlay : Window
{
    private Control? _originalControl;
    private Control? _clonedControl;
    
    public bool WasConfirmed { get; private set; }
    
    public TextEditorOverlay()
    {
        InitializeComponent();
        
#if DEBUG
        this.AttachDevTools();
#endif
        
        var okButton = this.FindControl<Button>("OkButton");
        var cancelButton = this.FindControl<Button>("CancelButton");
        
        if (okButton != null)
        {
            okButton.Click += OkButton_Click;
        }
        
        if (cancelButton != null)
        {
            cancelButton.Click += CancelButton_Click;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void SetEditorControl(Control originalControl)
    {
        _originalControl = originalControl;
        
        // Clone the control based on its type
        if (originalControl is TextBox textBox)
        {
            var clonedTextBox = new TextBox
            {
                Text = textBox.Text,
                TextWrapping = textBox.TextWrapping,
                AcceptsReturn = true,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _clonedControl = clonedTextBox;
        }
        else if (originalControl is TextEditor textEditor)
        {
            var clonedEditor = new TextEditor
            {
                Document = new TextDocument(textEditor.Text ?? string.Empty),
                ShowLineNumbers = textEditor.ShowLineNumbers,
                FontFamily = textEditor.FontFamily,
                Background = textEditor.Background,
                BorderBrush = textEditor.BorderBrush,
                BorderThickness = textEditor.BorderThickness,
                CornerRadius = textEditor.CornerRadius,
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            
            // Copy TextArea settings
            if (textEditor.TextArea?.TextView?.Margin != null)
            {
                clonedEditor.TextArea.TextView.Margin = textEditor.TextArea.TextView.Margin;
            }
            
            // Set up TextMate syntax highlighting from the Tag property
            if (textEditor.Tag is string syntax && !string.IsNullOrWhiteSpace(syntax))
            {
                var registry = new RegistryOptions(ThemeName.DarkPlus);
                var textMateInstallation = clonedEditor.InstallTextMate(registry);
                
                if (registry.GetLanguageByExtension("." + syntax) is { } languageByExtension)
                {
                    textMateInstallation.SetGrammar(registry.GetScopeByLanguageId(languageByExtension.Id));
                }
            }
            
            _clonedControl = clonedEditor;
        }
        
        var presenter = this.FindControl<ContentPresenter>("EditorContentPresenter");
        if (presenter != null && _clonedControl != null)
        {
            presenter.Content = _clonedControl;
        }
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        WasConfirmed = true;
        
        // Copy the text back to the original control
        if (_originalControl is TextBox originalTextBox && _clonedControl is TextBox clonedTextBox)
        {
            originalTextBox.Text = clonedTextBox.Text;
        }
        else if (_originalControl is TextEditor originalEditor && _clonedControl is TextEditor clonedEditor)
        {
            originalEditor.Text = clonedEditor.Text;
        }
        
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        WasConfirmed = false;
        Close();
    }
}
