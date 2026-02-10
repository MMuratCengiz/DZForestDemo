using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace NiziKit.Editor.Views;

public partial class DraggableValueEditor : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<DraggableValueEditor, string>(nameof(Label), "X");

    public static readonly StyledProperty<float> ValueProperty =
        AvaloniaProperty.Register<DraggableValueEditor, float>(nameof(Value), 0f,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<float> DragSensitivityProperty =
        AvaloniaProperty.Register<DraggableValueEditor, float>(nameof(DragSensitivity), 0.01f);

    public static readonly StyledProperty<string> StringFormatProperty =
        AvaloniaProperty.Register<DraggableValueEditor, string>(nameof(StringFormat), "F2");

    public static readonly StyledProperty<IBrush> LabelColorProperty =
        AvaloniaProperty.Register<DraggableValueEditor, IBrush>(nameof(LabelColor),
            new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)));

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public float Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public float DragSensitivity
    {
        get => GetValue(DragSensitivityProperty);
        set => SetValue(DragSensitivityProperty, value);
    }

    public string StringFormat
    {
        get => GetValue(StringFormatProperty);
        set => SetValue(StringFormatProperty, value);
    }

    public IBrush LabelColor
    {
        get => GetValue(LabelColorProperty);
        set => SetValue(LabelColorProperty, value);
    }

    private Border? _dragLabel;
    private TextBox? _valueTextBox;
    private TextBlock? _labelText;
    private bool _isDragging;
    private Point _dragStartPoint;
    private float _dragStartValue;

    public DraggableValueEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _dragLabel = this.FindControl<Border>("DragLabel");
        _valueTextBox = this.FindControl<TextBox>("ValueTextBox");
        _labelText = this.FindControl<TextBlock>("LabelText");

        if (_dragLabel != null)
        {
            _dragLabel.PointerPressed += OnDragLabelPointerPressed;
            _dragLabel.PointerMoved += OnDragLabelPointerMoved;
            _dragLabel.PointerReleased += OnDragLabelPointerReleased;
            _dragLabel.PointerCaptureLost += OnDragLabelPointerCaptureLost;
            _dragLabel.PointerEntered += OnDragLabelPointerEntered;
            _dragLabel.PointerExited += OnDragLabelPointerExited;
        }

        if (_valueTextBox != null)
        {
            _valueTextBox.LostFocus += OnValueTextBoxLostFocus;
            _valueTextBox.KeyDown += OnValueTextBoxKeyDown;
        }

        // Update bindings
        PropertyChanged += OnPropertyChanged;
        UpdateDisplay();
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ValueProperty || e.Property == StringFormatProperty)
        {
            UpdateDisplay();
        }
        else if (e.Property == LabelProperty && _labelText != null)
        {
            _labelText.Text = Label;
        }
        else if (e.Property == LabelColorProperty && _labelText != null)
        {
            _labelText.Foreground = LabelColor;
        }
    }

    private void UpdateDisplay()
    {
        if (_valueTextBox is { IsFocused: false })
        {
            _valueTextBox.Text = Value.ToString(StringFormat);
        }
        if (_labelText != null)
        {
            _labelText.Text = Label;
            _labelText.Foreground = LabelColor;
        }
    }

    private void OnDragLabelPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(_dragLabel).Properties.IsLeftButtonPressed)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(_dragLabel);
            _dragStartValue = Value;
            e.Pointer.Capture(_dragLabel);
            UpdateDragLabelBackground(true);
            e.Handled = true;
        }
    }

    private void OnDragLabelPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isDragging && _dragLabel != null)
        {
            var currentPoint = e.GetPosition(_dragLabel);
            var delta = (float)(currentPoint.X - _dragStartPoint.X);
            Value = _dragStartValue + delta * DragSensitivity;
            UpdateDisplay();
        }
    }

    private void OnDragLabelPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
        }
    }

    private void OnDragLabelPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _isDragging = false;
        UpdateDragLabelBackground(false);
    }

    private void OnDragLabelPointerEntered(object? sender, PointerEventArgs e)
    {
        UpdateDragLabelBackground(true);
    }

    private void OnDragLabelPointerExited(object? sender, PointerEventArgs e)
    {
        if (!_isDragging)
        {
            UpdateDragLabelBackground(false);
        }
    }

    private void UpdateDragLabelBackground(bool highlighted)
    {
        if (_dragLabel == null)
        {
            return;
        }

        var key = highlighted ? "EditorDragLabelHover" : "EditorDragLabelBg";
        if (this.TryFindResource(key, out var brush) && brush is IBrush b)
        {
            _dragLabel.Background = b;
        }
    }

    private void OnValueTextBoxLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        TryParseAndSetValue();
    }

    private void OnValueTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            TryParseAndSetValue();
            _valueTextBox?.SelectAll();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            UpdateDisplay();
            e.Handled = true;
        }
    }

    private void TryParseAndSetValue()
    {
        if (_valueTextBox != null && float.TryParse(_valueTextBox.Text, out var newValue))
        {
            Value = newValue;
        }
        UpdateDisplay();
    }
}
