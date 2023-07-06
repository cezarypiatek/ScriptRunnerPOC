using Avalonia.Media;
using System;
using System.Drawing;
using System.Linq;
using Avalonia.Controls.Documents;
using Avalonia.Media.Immutable;
using VtNetCore.VirtualTerminal;
using VtNetCore.VirtualTerminal.Enums;
using Brushes = Avalonia.Media.Brushes;
using Color = Avalonia.Media.Color;
using FontStyle = Avalonia.Media.FontStyle;
using Avalonia.Threading;
using DynamicData;

namespace ScriptRunner.GUI.ViewModels;

public class GuiTerminal: IVirtualTerminalController
{
    public void ClearChanges()
    {
    }

    public bool IsUtf8()
    {
        return true;
    }

    public bool IsVt52Mode()
    {
        return true;
    }

    public void Backspace()
    {
    }

    public void Bell()
    {
    }

    public void CarriageReturn()
    {
    }

    public void ClearScrollingRegion()
    {
    }

    public void ClearTab()
    {
    }

    public void ClearTabs()
    {
    }

    public void DeleteCharacter(int count)
    {
    }

    public void DeleteColumn(int count)
    {
        
    }

    public void DeleteLines(int count)
    {
        
    }

    public void DeviceStatusReport()
    {
        
    }

    public void Enable80132Mode(bool enable)
    {
        
    }

    public void Enable132ColumnMode(bool enable)
    {
        
    }

    public void EnableAlternateBuffer()
    {
        
    }

    public void EnableApplicationCursorKeys(bool enable)
    {
        
    }

    public void EnableAutoRepeatKeys(bool enable)
    {
        
    }

    public void EnableBlinkingCursor(bool enable)
    {
        
    }

    public void EnableLeftAndRightMarginMode(bool enable)
    {
        
    }

    public void EnableNormalBuffer()
    {
        
    }

    public void EnableOriginMode(bool enable)
    {
        
    }

    public void EnableReverseVideoMode(bool enable)
    {
        
    }

    public void EnableReverseWrapAroundMode(bool enable)
    {
        
    }

    public void EnableSmoothScrollMode(bool enable)
    {
        
    }

    public void EnableSgrMouseMode(bool enable)
    {
        
    }

    public void EnableWrapAroundMode(bool enable)
    {
        
    }

    public void EraseAbove(bool ignoreProtected)
    {
        
    }

    public void EraseAll(bool ignoreProtected)
    {
        //
    }

    public void EraseBelow(bool ignoreProtected)
    {
        
    }

    public void EraseCharacter(int count)
    {
        
    }

    public void EraseLine(bool ignoreProtected)
    {
        
    }

    public void EraseToEndOfLine(bool ignoreProtected)
    {
        
    }

    public void EraseToStartOfLine(bool ignoreProtected)
    {
        
    }

    public void FormFeed()
    {
        
    }

    public void FullReset()
    {
        
    }

    public void InsertBlanks(int count)
    {
        
    }

    public void InsertColumn(int count)
    {
        
    }

    public void InsertLines(int count)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _currentRun = null;
            
            RichOutput.Add(Enumerable.Range(0,count).Select(x=> new LineBreak()));
        });
    }

    public void InvokeCharacterSetMode(ECharacterSetMode mode)
    {
        
    }

    public void InvokeCharacterSetModeR(ECharacterSetMode mode)
    {
        
    }

    public void MoveCursorRelative(int x, int y)
    {
        
    }

    public void NewLine()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _currentRun = null;
            RichOutput.Add(new LineBreak());
        });
    }

    public void ProtectCharacter(int protect)
    {
        
    }

    private Run? _currentRun = null;
    public InlineCollection RichOutput { get; set; } = new();


    public void PutChar(char character)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_currentRun == null || _isStyleChanged)
            {
                _currentRun = new Run(character.ToString());
                _isStyleChanged = false;
                _currentRun.Foreground = CurrentForegroundColor;
                _currentRun.Background = CurrentBackgroundColor;
                if (IsBold)
                {
                    _currentRun.FontStyle = FontStyle.Oblique;
                }

                if (IsUnderline)
                {
                    _currentRun.TextDecorations.Add(new TextDecoration()
                    {
                        Location = TextDecorationLocation.Underline,
                    });
                }
                RichOutput.Add(_currentRun);
            }
            else
            {
                _currentRun.Text += character;
            }
        });

        
    }

    public void PutG2Char(char character)
    {
        
    }

    public void PutG3Char(char character)
    {
        
    }

    public void RepeatLastCharacter(int count)
    {
        
    }

    public void RequestDecPrivateMode(int mode)
    {
        
    }

    public void RequestStatusStringSetConformanceLevel()
    {
        
    }

    public void RequestStatusStringSetProtectionAttribute()
    {
        
    }

    public void ReportCursorPosition()
    {
        
    }

    public void ReportExtendedCursorPosition()
    {
        
    }

    public void RestoreCursor()
    {
        
    }

    public void RestoreEnableNormalBuffer()
    {
        
    }

    public void RestoreEnableSgrMouseMode()
    {
        
    }

    public void RestoreUseCellMotionMouseTracking()
    {
        
    }

    public void RestoreUseHighlightMouseTracking()
    {
        
    }

    public void RestoreBracketedPasteMode()
    {
        
    }

    public void RestoreCursorKeys()
    {
        
    }

    public void ReverseIndex()
    {
        
    }

    public void ReverseTab()
    {
        
    }

    public void SetAutomaticNewLine(bool enable)
    {
        
    }

    public void SaveBracketedPasteMode()
    {
        
    }

    public void SaveCursor()
    {
        
    }

    public void SaveCursorKeys()
    {
        
    }

    public void SaveEnableNormalBuffer()
    {
        
    }

    public void SaveEnableSgrMouseMode()
    {
        
    }

    public void SaveUseCellMotionMouseTracking()
    {
        
    }

    public void SaveUseHighlightMouseTracking()
    {
        
    }

    public void Scroll(int rows)
    {
        
    }

    public void ScrollAcross(int columns)
    {
        
    }

    public void SendDeviceAttributes()
    {
        
    }

    public void SendDeviceAttributesSecondary()
    {
        
    }

    public void SetAbsoluteRow(int line)
    {
        
    }

    public void SetBracketedPasteMode(bool enable)
    {
        
    }

    private bool IsBold
    {
        get => _isBold;
        set
        {
            _isBold = value;
            _isStyleChanged = true;
        }
    }

    private bool IsUnderline
    {
        get => _isUnderline;
        set
        {
            _isUnderline = value;
            _isStyleChanged = true;
        }
    }

    private IBrush CurrentForegroundColor
    {
        get => _currentForegroundColor;
        set
        {
            _currentForegroundColor = value;
            _isStyleChanged = true;
        }
    }

    private IBrush CurrentBackgroundColor
    {
        get => _currentBackgroundColor;
        set
        {
            _currentBackgroundColor = value;
            _isStyleChanged = true;
        }
    }

    private bool _isColorSwapped;
    private bool _isBold;
    private bool _isUnderline;
    private IBrush _currentForegroundColor = Brushes.White;
    private IBrush _currentBackgroundColor = Brushes.Transparent;

    private bool _isStyleChanged;

    public void SetCharacterAttribute(int parameter)
    {
        var code = (CharacterAttribute) parameter;
        switch (code)
        {
            case CharacterAttribute.Default:
                IsBold = false;
                IsUnderline = false;
                CurrentForegroundColor = Brushes.White;
                CurrentBackgroundColor = Brushes.Transparent;
                break;
            case CharacterAttribute.BoldBright:
                break;
            case CharacterAttribute.NoBoldBright:
                break;
            case CharacterAttribute.Underline:
                IsUnderline = true;
                break;
            case CharacterAttribute.NoUnderline:
                IsUnderline = false;
                break;
            case CharacterAttribute.Negative:
                _isColorSwapped = !_isColorSwapped;
                (CurrentBackgroundColor, CurrentForegroundColor) = (CurrentForegroundColor, CurrentBackgroundColor);
                break;
            case CharacterAttribute.Positive:
                if (_isColorSwapped)
                {
                    (CurrentBackgroundColor, CurrentForegroundColor) = (CurrentForegroundColor, CurrentBackgroundColor);
                    _isColorSwapped = false;
                }
                break;
            case CharacterAttribute.ForegroundBlack:
                CurrentForegroundColor = Brushes.Black;
                break;
            case CharacterAttribute.ForegroundRed:
                CurrentForegroundColor = Brushes.Red;
                break;
            case CharacterAttribute.ForegroundGreen:
                CurrentForegroundColor = Brushes.Green;
                break;
            case CharacterAttribute.ForegroundYellow:
                CurrentForegroundColor = Brushes.Yellow;
                break;
            case CharacterAttribute.ForegroundBlue:
                CurrentForegroundColor = Brushes.Blue;
                break;
            case CharacterAttribute.ForegroundMagenta:
                CurrentForegroundColor = Brushes.Magenta;
                break;
            case CharacterAttribute.ForegroundCyan:
                CurrentForegroundColor = Brushes.Cyan;
                break;
            case CharacterAttribute.ForegroundWhite:
                CurrentForegroundColor = Brushes.White;
                break;
            case CharacterAttribute.ForegroundExtended:
                break;
            case CharacterAttribute.ForegroundDefault:
                CurrentForegroundColor = Brushes.White;
                break;
            case CharacterAttribute.BackgroundBlack:
                CurrentBackgroundColor = Brushes.Black;
                break;
            case CharacterAttribute.BackgroundRed:
                CurrentBackgroundColor = Brushes.Red;
                break;
            case CharacterAttribute.BackgroundGreen:
                CurrentBackgroundColor = Brushes.Green;
                break;
            case CharacterAttribute.BackgroundYellow:
                CurrentBackgroundColor = Brushes.Yellow;
                break;
            case CharacterAttribute.BackgroundBlue:
                CurrentBackgroundColor = Brushes.Blue;
                break;
            case CharacterAttribute.BackgroundMagenta:
                CurrentBackgroundColor = Brushes.Magenta;
                break;
            case CharacterAttribute.BackgroundCyan:
                CurrentBackgroundColor = Brushes.Cyan;
                break;
            case CharacterAttribute.BackgroundWhite:
                CurrentBackgroundColor = Brushes.White;
                break;
            case CharacterAttribute.BackgroundExtended:
                break;
            case CharacterAttribute.BackgroundDefault:
                CurrentBackgroundColor = Brushes.Black;
                break;
            case CharacterAttribute.BrightForegroundBlack:
                CurrentForegroundColor = Brushes.Gray;
                break;
            case CharacterAttribute.BrightForegroundRed:
                CurrentForegroundColor = Brushes.Pink;
                break;
            case CharacterAttribute.BrightForegroundGreen:
                CurrentForegroundColor = Brushes.LightGreen;
                break;
            case CharacterAttribute.BrightForegroundYellow:
                CurrentForegroundColor = Brushes.LightYellow;
                break;
            case CharacterAttribute.BrightForegroundBlue:
                CurrentForegroundColor = Brushes.LightBlue;
                break;
            case CharacterAttribute.BrightForegroundMagenta:
                CurrentForegroundColor = Brushes.LightSalmon;
                break;
            case CharacterAttribute.BrightForegroundCyan:
                CurrentForegroundColor = Brushes.LightCyan;
                break;
            case CharacterAttribute.BrightForegroundWhite:
                CurrentForegroundColor = Brushes.WhiteSmoke;
                break;
            case CharacterAttribute.BrightBackgroundBlack:
                CurrentForegroundColor = Brushes.Black;
                break;
            case CharacterAttribute.BrightBackgroundRed:
                CurrentForegroundColor = Brushes.Red;
                break;
            case CharacterAttribute.BrightBackgroundGreen:
                CurrentForegroundColor = Brushes.Green;
                break;
            case CharacterAttribute.BrightBackgroundYellow:
                CurrentForegroundColor = Brushes.Yellow;;
                break;
            case CharacterAttribute.BrightBackgroundBlue:
                CurrentForegroundColor = Brushes.Blue;
                break;
            case CharacterAttribute.BrightBackgroundMagenta:
                CurrentForegroundColor = Brushes.Magenta;
                break;
            case CharacterAttribute.BrightBackgroundCyan:
                CurrentForegroundColor = Brushes.Cyan;
                break;
            case CharacterAttribute.BrightBackgroundWhite:
                CurrentForegroundColor = Brushes.White;
                break;
            
        }
    }

    public void SetCharacterSet(ECharacterSet characterSet, ECharacterSetMode mode)
    {
        
    }

    public void SetCharacterSize(ECharacterSize size)
    {
        
    }
    ///
    public void SetConformanceLevel(int level, bool eightBit)
    {
        
    }

    public void SetCursorPosition(int column, int row)
    {
        //
    }

    public void SetCursorStyle(ECursorShape shape, bool blink)
    {
        
    }

    public void SetEndOfGuardedArea()
    {
        
    }

    public void SetErasureMode(bool enabled)
    {
        
    }

    public void SetGuardedAreaTransferMode(bool enabled)
    {
        
    }

    public void SetInsertReplaceMode(EInsertReplaceMode mode)
    {
        
    }

    public void SetIso8613PaletteBackground(int paletteEntry)
    {
        
    }

    public void SetIso8613PaletteForeground(int paletteEntry)
    {
        
    }

    public void SetLatin1()
    {
        
    }

    public void SetLeftAndRightMargins(int left, int right)
    {
        
    }

    public void SetKeypadType(EKeypadType type)
    {
        
    }

    public void SetRgbBackgroundColor(int red, int green, int blue)
    {
        CurrentBackgroundColor = new ImmutableSolidColorBrush( Color.FromRgb((byte)red, (byte)green, (byte)blue));

    }

    public void SetRgbForegroundColor(int red, int green, int blue)
    {
        CurrentForegroundColor = new ImmutableSolidColorBrush(Color.FromRgb((byte)red, (byte)green, (byte)blue));
    }

    public void SetScrollingRegion(int top, int bottom)
    {
        
    }

    public void SetSendFocusInAndFocusOutEvents(bool enabled)
    {
        
    }

    public void SetStartOfGuardedArea()
    {
        
    }

    public void SetUseAllMouseTracking(bool enabled)
    {
        
    }

    public void SetUTF8()
    {
        
    }

    public void SetUtf8MouseMode(bool enabled)
    {
        
    }

    public void SetVt52AlternateKeypadMode(bool enabled)
    {
        
    }

    public void SetVt52GraphicsMode(bool enabled)
    {
        
    }

    public void SetVt52Mode(bool enabled)
    {
        
    }

    public void SetWindowTitle(string title)
    {
        //
    }

    public void SetX10SendMouseXYOnButton(bool enabled)
    {
        
    }

    public void SetX11SendMouseXYOnButton(bool enabled)
    {
        
    }

    public void ShiftIn()
    {
        
    }

    public void ShiftOut()
    {
        
    }

    public void ShowCursor(bool show)
    {
        //
    }

    public void Tab()
    {
        
    }

    public void TabSet()
    {
        
    }

    public void UseCellMotionMouseTracking(bool enable)
    {
        
    }

    public void UseHighlightMouseTracking(bool enable)
    {
        
    }

    public void VerticalTab()
    {
        
    }

    public void Vt52EnterAnsiMode()
    {
        
    }

    public void Vt52Identify()
    {
        
    }
}

public enum CharacterAttribute
{
    /// <summary>
    /// Returns all attributes to the default state prior to modification
    /// </summary>
    Default = 0,
    /// <summary>
    /// Applies brightness/intensity flag to foreground color
    /// </summary>
    BoldBright = 1,
    /// <summary>
    /// Removes brightness/intensity flag from foreground color
    /// </summary>
    NoBoldBright = 22,
    /// <summary>
    /// Adds underline
    /// </summary>
    Underline = 4,
    /// <summary>
    /// Removes underline
    /// </summary>
    NoUnderline = 24,
    /// <summary>
    /// Swaps foreground and background colors
    /// </summary>
    Negative = 7,
    /// <summary>
    /// Returns foreground/background to normal
    /// </summary>
    Positive = 27,
    /// <summary>
    /// Applies non-bold/bright black to foreground
    /// </summary>
    ForegroundBlack = 30,
    /// <summary>
    /// Applies non-bold/bright red to foreground
    /// </summary>
    ForegroundRed = 31,
    /// <summary>
    /// Applies non-bold/bright green to foreground
    /// </summary>
    ForegroundGreen = 32,
    /// <summary>
    /// Applies non-bold/bright yellow to foreground
    /// </summary>
    ForegroundYellow = 33,
    /// <summary>
    /// Applies non-bold/bright blue to foreground
    /// </summary>
    ForegroundBlue = 34,
    /// <summary>
    /// Applies non-bold/bright magenta to foreground
    /// </summary>
    ForegroundMagenta = 35,
    /// <summary>
    /// Applies non-bold/bright cyan to foreground
    /// </summary>
    ForegroundCyan = 36,
    /// <summary>
    /// Applies non-bold/bright white to foreground
    /// </summary>
    ForegroundWhite = 37,
    /// <summary>
    /// Applies extended color value to the foreground (see details below)
    /// </summary>
    ForegroundExtended = 38,
    /// <summary>
    /// Applies only the foreground portion of the defaults (see 0)
    /// </summary>
    ForegroundDefault = 39,
    /// <summary>
    /// Applies non-bold/bright black to background
    /// </summary>
    BackgroundBlack = 40,
    /// <summary>
    /// Applies non-bold/bright red to background
    /// </summary>
    BackgroundRed = 41,
    /// <summary>
    /// Applies non-bold/bright green to background
    /// </summary>
    BackgroundGreen = 42,
    /// <summary>
    /// Applies non-bold/bright yellow to background
    /// </summary>
    BackgroundYellow = 43,
    /// <summary>
    /// Applies non-bold/bright blue to background
    /// </summary>
    BackgroundBlue = 44,
    /// <summary>
    /// Applies non-bold/bright magenta to background
    /// </summary>
    BackgroundMagenta = 45,
    /// <summary>
    /// Applies non-bold/bright cyan to background
    /// </summary>
    BackgroundCyan = 46,
    /// <summary>
    /// Applies non-bold/bright white to background
    /// </summary>
    BackgroundWhite = 47,
    /// <summary>
    /// Applies extended color value to the background (see details below)
    /// </summary>
    BackgroundExtended = 48,
    /// <summary>
    /// Applies only the background portion of the defaults (see 0)
    /// </summary>
    BackgroundDefault = 49,
    /// <summary>
    /// Applies bold/bright black to foreground
    /// </summary>
    BrightForegroundBlack = 90,
    /// <summary>
    /// Applies bold/bright red to foreground
    /// </summary>
    BrightForegroundRed = 91,
    /// <summary>
    /// Applies bold/bright green to foreground
    /// </summary>
    BrightForegroundGreen = 92,
    /// <summary>
    /// Applies bold/bright yellow to foreground
    /// </summary>
    BrightForegroundYellow = 93,
    /// <summary>
    /// Applies bold/bright blue to foreground
    /// </summary>
    BrightForegroundBlue = 94,
    /// <summary>
    /// Applies bold/bright magenta to foreground
    /// </summary>
    BrightForegroundMagenta = 95,
    /// <summary>
    /// Applies bold/bright cyan to foreground
    /// </summary>
    BrightForegroundCyan = 96,
    /// <summary>
    /// Applies bold/bright white to foreground
    /// </summary>
    BrightForegroundWhite = 97,
    /// <summary>
    /// Applies bold/bright black to background
    /// </summary>
    BrightBackgroundBlack = 100,
    /// <summary>
    /// Applies bold/bright red to background
    /// </summary>
    BrightBackgroundRed = 101,
    /// <summary>
    /// Applies bold/bright green to background
    /// </summary>
    BrightBackgroundGreen = 102,
    /// <summary>
    /// Applies bold/bright yellow to background
    /// </summary>
    BrightBackgroundYellow = 103,
    /// <summary>
    /// Applies bold/bright blue to background
    /// </summary>
    BrightBackgroundBlue = 104,
    /// <summary>
    /// Applies bold/bright magenta to background
    /// </summary>
    BrightBackgroundMagenta = 105,
    /// <summary>
    /// Applies bold/bright cyan to background
    /// </summary>
    BrightBackgroundCyan = 106,
    /// <summary>
    /// Applies bold/bright white to background
    /// </summary>
    BrightBackgroundWhite = 107
}