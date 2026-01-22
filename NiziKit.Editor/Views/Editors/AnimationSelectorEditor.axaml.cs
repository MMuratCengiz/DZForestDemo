using System.Reflection;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NiziKit.Assets;

namespace NiziKit.Editor.Views.Editors;

public partial class AnimationSelectorEditor : UserControl
{
    private ComboBox? _animationComboBox;
    private bool _isUpdating;

    public object? Instance { get; set; }
    public PropertyInfo? Property { get; set; }
    public PropertyInfo? SkeletonProperty { get; set; }
    public bool IsReadOnly { get; set; }
    public Action? OnValueChanged { get; set; }

    public AnimationSelectorEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _animationComboBox = this.FindControl<ComboBox>("AnimationComboBox");

        if (_animationComboBox != null)
        {
            _animationComboBox.SelectionChanged += OnSelectionChanged;
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        PopulateAnimations();
        UpdateReadOnly();
    }

    private void PopulateAnimations()
    {
        if (_animationComboBox == null || Instance == null || SkeletonProperty == null)
        {
            return;
        }

        _isUpdating = true;
        try
        {
            var items = new List<string> { "(None)" };

            var skeleton = SkeletonProperty.GetValue(Instance) as Skeleton;
            if (skeleton != null)
            {
                foreach (var animName in skeleton.AnimationNames)
                {
                    items.Add(animName);
                }
            }

            _animationComboBox.ItemsSource = items;

            var currentValue = Property?.GetValue(Instance) as string;
            if (string.IsNullOrEmpty(currentValue))
            {
                _animationComboBox.SelectedIndex = 0;
            }
            else
            {
                var index = items.IndexOf(currentValue);
                _animationComboBox.SelectedIndex = index >= 0 ? index : 0;
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating || IsReadOnly || _animationComboBox == null || Property == null || Instance == null)
        {
            return;
        }

        var selectedItem = _animationComboBox.SelectedItem as string;
        var newValue = selectedItem == "(None)" ? null : selectedItem;

        Property.SetValue(Instance, newValue);
        OnValueChanged?.Invoke();
    }

    private void UpdateReadOnly()
    {
        if (_animationComboBox != null)
        {
            _animationComboBox.IsEnabled = !IsReadOnly;
        }
    }

    public void RefreshAnimations()
    {
        PopulateAnimations();
    }
}
