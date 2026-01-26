using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NiziKit.Editor.Views.Editors;

public partial class Vector4Editor : UserControl
{
    public static readonly StyledProperty<Vector4> ValueProperty =
        AvaloniaProperty.Register<Vector4Editor, Vector4>(nameof(Value), Vector4.Zero,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<Vector4Editor, bool>(nameof(IsReadOnly), false);

    private DraggableValueEditor? _xEditor;
    private DraggableValueEditor? _yEditor;
    private DraggableValueEditor? _zEditor;
    private DraggableValueEditor? _wEditor;
    private bool _isUpdating;

    public Vector4 Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public Action<Vector4>? OnValueChanged { get; set; }

    public Vector4Editor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _xEditor = this.FindControl<DraggableValueEditor>("XEditor");
        _yEditor = this.FindControl<DraggableValueEditor>("YEditor");
        _zEditor = this.FindControl<DraggableValueEditor>("ZEditor");
        _wEditor = this.FindControl<DraggableValueEditor>("WEditor");

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
        if (_wEditor != null)
        {
            _wEditor.PropertyChanged += OnEditorValueChanged;
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

            if (_zEditor != null)
            {
                _zEditor.Value = value.Z;
            }

            if (_wEditor != null)
            {
                _wEditor.Value = value.W;
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

        if (_wEditor != null)
        {
            _wEditor.IsEnabled = !IsReadOnly;
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
            var w = _wEditor?.Value ?? 0f;
            var newValue = new Vector4(x, y, z, w);
            Value = newValue;
            OnValueChanged?.Invoke(newValue);
        }
        finally
        {
            _isUpdating = false;
        }
    }
}
