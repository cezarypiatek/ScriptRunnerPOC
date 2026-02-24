using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace ScriptRunner.GUI.Views.Controls
{
    public partial class TimePickerInput : UserControl
    {
        private TextBox _timeTextBox;
        private Button _openPopupButton;
        private Popup _timePopup;
        private Border _hourBorder;
        private Border _minuteBorder;
        private TextBlock _hourText;
        private TextBlock _minuteText;
        private Border _amBorder;
        private Border _pmBorder;
        private TextBlock _amText;
        private TextBlock _pmText;
        private Line _clockHand;
        private Ellipse _selectionCircle;
        private Canvas _numbersCanvas;
        private Panel _clockFacePanel;
        private Button _cancelBtn;
        private Button _okBtn;

        private TimeSpan? _selectedTime;
        public TimeSpan? SelectedTime
        {
            get => _selectedTime;
            set
            {
                if (_selectedTime != value)
                {
                    _selectedTime = value;
                    UpdateText();
                }
            }
        }

        private bool _isHourMode = true;
        private int _currentHour = 7;
        private int _currentMinute = 0;
        private bool _isAm = true;
        private bool _isDragging = false;

        public TimePickerInput()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _timeTextBox = this.FindControl<TextBox>("TimeTextBox");
            _openPopupButton = this.FindControl<Button>("OpenPopupButton");
            _timePopup = this.FindControl<Popup>("TimePopup");
            _hourBorder = this.FindControl<Border>("HourBorder");
            _minuteBorder = this.FindControl<Border>("MinuteBorder");
            _hourText = this.FindControl<TextBlock>("HourText");
            _minuteText = this.FindControl<TextBlock>("MinuteText");
            _amBorder = this.FindControl<Border>("AmBorder");
            _pmBorder = this.FindControl<Border>("PmBorder");
            _amText = this.FindControl<TextBlock>("AmText");
            _pmText = this.FindControl<TextBlock>("PmText");
            _clockHand = this.FindControl<Line>("ClockHand");
            _selectionCircle = this.FindControl<Ellipse>("SelectionCircle");
            _numbersCanvas = this.FindControl<Canvas>("NumbersCanvas");
            _clockFacePanel = this.FindControl<Panel>("ClockFacePanel");
            _cancelBtn = this.FindControl<Button>("CancelBtn");
            _okBtn = this.FindControl<Button>("OkBtn");

            _timeTextBox.LostFocus += TimeTextBox_LostFocus;
            _timeTextBox.KeyDown += TimeTextBox_KeyDown;
            _timeTextBox.AddHandler(InputElement.TextInputEvent, (s, ev) => ev.Handled = true, RoutingStrategies.Tunnel);

            _openPopupButton.Click += (s, e) => OpenPopup();
            
            _hourBorder.PointerPressed += (s, e) => SetMode(true);
            _minuteBorder.PointerPressed += (s, e) => SetMode(false);
            
            _amBorder.PointerPressed += (s, e) => SetAmPm(true);
            _pmBorder.PointerPressed += (s, e) => SetAmPm(false);
            
            _clockFacePanel.PointerPressed += Clock_PointerPressed;
            _clockFacePanel.PointerMoved += Clock_PointerMoved;
            _clockFacePanel.PointerReleased += Clock_PointerReleased;
            
            _cancelBtn.Click += (s, e) => _timePopup.IsOpen = false;
            _okBtn.Click += (s, e) => ApplyTime();

            SetMode(true);
            DrawNumbers();
        }

        private void TimeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TimeTextBox_LostFocus(sender, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            var text = _timeTextBox.Text ?? "00:00";
            if (text.Length != 5) text = "00:00";

            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                e.Handled = true;

                if (!TryParseTime(text, out var currentTime))
                {
                    currentTime = SelectedTime ?? TimeSpan.Zero;
                }

                int caret = _timeTextBox.CaretIndex;
                bool isHour = caret <= 2;

                int hours = currentTime.Hours;
                int minutes = currentTime.Minutes;
                int delta = e.Key == Key.Up ? 1 : -1;

                if (isHour)
                {
                    hours += delta;
                    if (hours < 0) hours = 23;
                    if (hours > 23) hours = 0;
                }
                else
                {
                    minutes += delta;
                    if (minutes < 0) minutes = 59;
                    if (minutes > 59) minutes = 0;
                }

                SelectedTime = new TimeSpan(hours, minutes, 0);
                UpdateText();
                
                // Restore caret
                _timeTextBox.CaretIndex = caret;
                return;
            }

            bool isNumPad = e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9;
            bool isD = e.Key >= Key.D0 && e.Key <= Key.D9;

            if (isNumPad || isD)
            {
                e.Handled = true;
                
                int digit = isNumPad ? e.Key - Key.NumPad0 : e.Key - Key.D0;
                int caret = _timeTextBox.CaretIndex;

                if (caret == 2) caret++; // Skip colon

                if (caret < 5)
                {
                    var chars = text.ToCharArray();
                    chars[caret] = (char)('0' + digit);
                    _timeTextBox.Text = new string(chars);

                    caret++;
                    if (caret == 2) caret++; // Skip colon again if moving past it
                    
                    _timeTextBox.CaretIndex = caret;
                    
                    // Trigger parse if valid time so that clock updates instantly
                    if (TryParseTime(_timeTextBox.Text, out var parsed))
                    {
                        _selectedTime = parsed;
                        
                        _currentHour = parsed.Hours;
                        _currentMinute = parsed.Minutes;
                        
                        if (_currentHour >= 12)
                        {
                            _isAm = false;
                            if (_currentHour > 12) _currentHour -= 12;
                        }
                        else
                        {
                            _isAm = true;
                            if (_currentHour == 0) _currentHour = 12;
                        }
                        
                        SetAmPm(_isAm);
                        UpdateDisplay();
                    }
                }
                return;
            }

            if (e.Key == Key.Back)
            {
                e.Handled = true;
                int caret = _timeTextBox.CaretIndex;

                if (caret > 0)
                {
                    caret--;
                    if (caret == 2) caret--; // Skip colon

                    var chars = text.ToCharArray();
                    chars[caret] = '0';
                    _timeTextBox.Text = new string(chars);
                    _timeTextBox.CaretIndex = caret;
                }
                return;
            }

            if (e.Key == Key.Delete)
            {
                e.Handled = true;
                int caret = _timeTextBox.CaretIndex;

                if (caret == 2) caret++; // Skip colon
                
                if (caret < 5)
                {
                    var chars = text.ToCharArray();
                    chars[caret] = '0';
                    _timeTextBox.Text = new string(chars);
                    _timeTextBox.CaretIndex = caret;
                }
                return;
            }

            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Tab || e.Key == Key.Escape)
            {
                // Let the TextBox handle navigation
                return;
            }

            // Block any other key stroke (letters, symbols)
            e.Handled = true;
        }

        private bool TryParseTime(string text, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var parts = text.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
            {
                if (h >= 0 && h <= 23 && m >= 0 && m <= 59)
                {
                    time = new TimeSpan(h, m, 0);
                    return true;
                }
            }
            return false;
        }

        private void TimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (TryParseTime(_timeTextBox.Text, out var parsed))
            {
                SelectedTime = parsed;
            }
            else
            {
                UpdateText(); // Revert to previous valid value
            }
        }

        private void OpenPopup()
        {
            if (SelectedTime.HasValue)
            {
                _currentHour = SelectedTime.Value.Hours;
                _currentMinute = SelectedTime.Value.Minutes;
                
                if (_currentHour == 0)
                {
                    _currentHour = 12;
                    _isAm = true;
                }
                else if (_currentHour == 12)
                {
                    _isAm = false;
                }
                else if (_currentHour > 12)
                {
                    _currentHour -= 12;
                    _isAm = false;
                }
                else
                {
                    _isAm = true;
                }
            }
            else
            {
                var now = DateTime.Now;
                _currentHour = now.Hour;
                _currentMinute = now.Minute;
                
                if (_currentHour == 0)
                {
                    _currentHour = 12;
                    _isAm = true;
                }
                else if (_currentHour == 12)
                {
                    _isAm = false;
                }
                else if (_currentHour > 12)
                {
                    _currentHour -= 12;
                    _isAm = false;
                }
                else
                {
                    _isAm = true;
                }
            }

            SetAmPm(_isAm);
            SetMode(true);
            UpdateDisplay();
            _timePopup.IsOpen = true;
        }

        private void ApplyTime()
        {
            int h = _currentHour;
            if (_isAm && h == 12) h = 0;
            if (!_isAm && h < 12) h += 12;
            
            SelectedTime = new TimeSpan(h, _currentMinute, 0);
            _timePopup.IsOpen = false;
        }

        private void UpdateText()
        {
            if (SelectedTime.HasValue)
            {
                _timeTextBox.Text = SelectedTime.Value.ToString(@"hh\:mm");
            }
            else
            {
                _timeTextBox.Text = string.Empty;
            }
        }

        private void SetMode(bool isHour)
        {
            _isHourMode = isHour;
            
            if (isHour)
            {
                _hourBorder.Background = new SolidColorBrush(Color.Parse("#7B1FA2")); // Active purple
                _minuteBorder.Background = new SolidColorBrush(Color.Parse("#38383B")); // Inactive grey
            }
            else
            {
                _hourBorder.Background = new SolidColorBrush(Color.Parse("#38383B"));
                _minuteBorder.Background = new SolidColorBrush(Color.Parse("#7B1FA2"));
            }
            
            DrawNumbers();
            UpdateDisplay();
        }

        private void SetAmPm(bool isAm)
        {
            _isAm = isAm;
            if (isAm)
            {
                _amBorder.Background = new SolidColorBrush(Color.Parse("#7B1FA2"));
                _amText.Foreground = Brushes.White;
                _pmBorder.Background = Brushes.Transparent;
                _pmText.Foreground = new SolidColorBrush(Color.Parse("#A0A0A0"));
            }
            else
            {
                _pmBorder.Background = new SolidColorBrush(Color.Parse("#7B1FA2"));
                _pmText.Foreground = Brushes.White;
                _amBorder.Background = Brushes.Transparent;
                _amText.Foreground = new SolidColorBrush(Color.Parse("#A0A0A0"));
            }
        }

        private void DrawNumbers()
        {
            _numbersCanvas.Children.Clear();
            
            int count = _isHourMode ? 12 : 60;
            int step = _isHourMode ? 1 : 5;
            double radius = 100;
            double centerX = 120;
            double centerY = 120;

            for (int i = 0; i < count; i += step)
            {
                int val = i;
                if (_isHourMode && val == 0) val = 12;
                
                string text = val.ToString();
                if (!_isHourMode) text = text.PadLeft(2, '0');

                double angle = (i * 360.0 / count) - 90;
                double rad = angle * Math.PI / 180.0;
                
                double x = centerX + radius * Math.Cos(rad);
                double y = centerY + radius * Math.Sin(rad);

                var tb = new TextBlock
                {
                    Text = text,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };

                // Measure size roughly to center it
                Canvas.SetLeft(tb, x - 8);
                Canvas.SetTop(tb, y - 10);
                
                _numbersCanvas.Children.Add(tb);
            }
        }

        private void UpdateDisplay()
        {
            _hourText.Text = _currentHour.ToString("00");
            _minuteText.Text = _currentMinute.ToString("00");

            int val = _isHourMode ? _currentHour : _currentMinute;
            int max = _isHourMode ? 12 : 60;
            
            double angle = (val * 360.0 / max) - 90;
            double rad = angle * Math.PI / 180.0;
            
            double radius = 100; // Hand length
            double centerX = 120;
            double centerY = 120;
            
            double endX = centerX + radius * Math.Cos(rad);
            double endY = centerY + radius * Math.Sin(rad);

            _clockHand.EndPoint = new Point(endX, endY);
            
            _selectionCircle.Margin = new Avalonia.Thickness(endX - 18, endY - 18, 0, 0);
        }

        private void Clock_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            _isDragging = true;
            e.Pointer.Capture(_clockFacePanel);
            HandleClockInput(e.GetPosition(_clockFacePanel));
            e.Handled = true;
        }

        private void Clock_PointerMoved(object sender, PointerEventArgs e)
        {
            if (_isDragging)
            {
                HandleClockInput(e.GetPosition(_clockFacePanel));
            }
        }

        private void Clock_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                e.Pointer.Capture(null);
                HandleClockInput(e.GetPosition(_clockFacePanel));
                
                if (_isHourMode)
                {
                    // Auto switch to minutes after selecting hour
                    SetMode(false);
                }
            }
        }

        private void HandleClockInput(Point p)
        {
            double dx = p.X - 120;
            double dy = p.Y - 120;
            double angle = Math.Atan2(dy, dx) * 180 / Math.PI + 90;
            if (angle < 0) angle += 360;

            if (_isHourMode)
            {
                int hour = (int)Math.Round(angle / 30.0);
                if (hour == 0) hour = 12;
                if (hour > 12) hour -= 12;
                _currentHour = hour;
            }
            else
            {
                int minute = (int)Math.Round(angle / 6.0);
                if (minute >= 60) minute = 0;
                _currentMinute = minute;
            }

            UpdateDisplay();
        }
    }
}