using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NiziKit.Editor.Views.Editors;

public partial class Vector3Editor : UserControl
{
    public static readonly StyledProperty<Vector3> ValueProperty =
        AvaloniaProperty.Register<Vector3Editor, Vector3>(nameof(Value), Vector3.Zero,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<Vector3Editor, bool>(nameof(IsReadOnly), false);

    private DraggableValueEditor? _xEditor;
    private DraggableValueEditor? _yEditor;
    private DraggableValueEditor? _zEditor;
    private bool _isUpdating;

    public Vector3 Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public Action<Vector3>? OnValueChanged { get; set; }

    public Vector3Editor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _xEditor = this.FindControl<DraggableValueEditor>("XEditor");
        _yEditor = this.FindControl<DraggableValueEditor>("YEditor");
        _zEditor = this.FindControl<DraggableValueEditor>("ZEditor");

        if (_xEditor != null)
        {
            _xEditor.PropertyChanged += OnEditorValueChanged;
        }
        if (_yEditor != null)
        {
            _yEditor.PropertyChanged += OnEditorValueChanged;
        }
        if (_zEditor != null)
        {
            _zEditor.PropertyChanged += OnEditorValueChanged;
        }

        this.PropertyChanged += OnThisPropertyChanged;
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

            if (_zEditor != null)
            {
                _zEditor.Value = value.Z;
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

        if (_zEditor != null)
        {
            _zEditor.IsEnabled = !IsReadOnly;
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
            var z = _zEditor?.Value ?? 0f;
            var newValue = new Vector3(x, y, z);
            Value = newValue;
            OnValueChanged?.Invoke(newValue);
        }
        finally
        {
            _isUpdating = false;
        }
    }
}
