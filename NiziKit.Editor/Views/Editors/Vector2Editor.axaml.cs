using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NiziKit.Editor.Views.Editors;

public partial class Vector2Editor : UserControl
{
    public static readonly StyledProperty<Vector2> ValueProperty =
        AvaloniaProperty.Register<Vector2Editor, Vector2>(nameof(Value), Vector2.Zero,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<Vector2Editor, bool>(nameof(IsReadOnly), false);

    private DraggableValueEditor? _xEditor;
    private DraggableValueEditor? _yEditor;
    private bool _isUpdating;

    public Vector2 Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public Action<Vector2>? OnValueChanged { get; set; }

    public Vector2Editor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _xEditor = this.FindControl<DraggableValueEditor>("XEditor");
        _yEditor = this.FindControl<DraggableValueEditor>("YEditor");

        if (_xEditor != null)
        {
            _xEditor.PropertyChanged += OnEditorValueChanged;
        }
        if (_yEditor != null)
        {
            _yEditor.PropertyChanged += OnEditorValueChanged;
        }

        PropertyChanged += OnThisPropertyChanged;
        UpdateEditors();
    }

    private void OnThisPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ValueProperty)
        {
            UpdateEditors();
        }
        else if (e.Property == IsReadOnlyProperty)
        {
            UpdateReadOnly();
        }
    }

    private void UpdateEditors()
    {
        if (_isUpdating)
        {
            return;
        }

        _isUpdating = true;

        try
        {
            var value = Value;
            if (_xEditor != null)
            {
                _xEditor.Value = value.X;
            }

            if (_yEditor != null)
            {
                _yEditor.Value = value.Y;
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void UpdateReadOnly()
    {
        if (_xEditor != null)
        {
            _xEditor.IsEnabled = !IsReadOnly;
        }

        if (_yEditor != null)
        {
            _yEditor.IsEnabled = !IsReadOnly;
        }
    }

    private void OnEditorValueChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_isUpdating || e.Property.Name != nameof(DraggableValueEditor.Value))
        {
            return;
        }

        if (IsReadOnly)
        {
            return;
        }

        _isUpdating = true;
        try
        {
            var x = _xEditor?.Value ?? 0f;
            var y = _yEditor?.Value ?? 0f;
            var newValue = new Vector2(x, y);
            Value = newValue;
            OnValueChanged?.Invoke(newValue);
        }
        finally
        {
            _isUpdating = false;
        }
    }
}
