using System.Collections;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using NiziKit.Animation;
using NiziKit.Components;
using NiziKit.Editor.Services;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views.Editors;

public partial class AnimationListEditor : UserControl
{
    private TextBlock? _countLabel;
    private Button? _addButton;
    private ItemsControl? _itemsControl;

    public object? Instance { get; set; }
    public PropertyInfo? Property { get; set; }
    public AssetBrowserService? AssetBrowser { get; set; }
    public EditorViewModel? EditorViewModel { get; set; }
    public Action? OnValueChanged { get; set; }
    public Action? OnAnimationsChanged { get; set; }
    public bool IsReadOnly { get; set; }

    public AnimationListEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _countLabel = this.FindControl<TextBlock>("CountLabel");
        _addButton = this.FindControl<Button>("AddButton");
        _itemsControl = this.FindControl<ItemsControl>("ItemsControl");

        if (_addButton != null)
        {
            _addButton.Click += OnAddClicked;
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Rebuild();
    }

    public void Rebuild()
    {
        if (Instance == null || Property == null || _itemsControl == null)
        {
            return;
        }

        var list = Property.GetValue(Instance) as IList;
        if (list == null)
        {
            return;
        }

        UpdateCountLabel(list.Count);
        UpdateAddButton();

        var controls = new List<Control>();

        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] is AnimationEntry entry)
            {
                var index = i;
                var itemPanel = CreateItemPanel(entry, index, list);
                controls.Add(itemPanel);
            }
        }

        _itemsControl.ItemsSource = controls;
    }

    private Control CreateItemPanel(AnimationEntry entry, int index, IList list)
    {
        var panel = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Margin = new Thickness(0, 2)
        };

        if (entry.IsExternal)
        {
            var badge = new Border
            {
                Background = this.FindResource("SystemAccentColorLight1") as Avalonia.Media.IBrush,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(4, 2),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = "Ext",
                    Classes = { "caption" }
                }
            };
            Grid.SetColumn(badge, 0);
            panel.Children.Add(badge);
        }

        var nameText = new TextBlock
        {
            Text = entry.IsExternal ? $"{entry.Name} ({entry.SourceRef})" : entry.Name,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(nameText, 1);
        panel.Children.Add(nameText);

        if (!IsReadOnly)
        {
            var removeButton = new Button
            {
                Content = "X",
                Padding = new Thickness(6, 2),
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            removeButton.Click += (s, e) => RemoveItem(index, list);
            Grid.SetColumn(removeButton, 2);
            panel.Children.Add(removeButton);
        }

        return panel;
    }

    private void OnAddClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Instance == null || Property == null || IsReadOnly || EditorViewModel == null)
        {
            return;
        }

        var list = Property.GetValue(Instance) as IList;
        if (list == null)
        {
            return;
        }

        EditorViewModel.OpenAssetPicker(AssetRefType.Animation, null, null, asset =>
        {
            if (asset != null)
            {
                var slashIndex = asset.Name.IndexOf('/');
                var animName = slashIndex > 0 ? asset.Name[(slashIndex + 1)..] : asset.Name;
                
                var entry = AnimationEntry.External(animName, asset.FullReference);
                list.Add(entry);
                OnValueChanged?.Invoke();
                OnAnimationsChanged?.Invoke();
                Rebuild();
            }
        });
    }

    private void RemoveItem(int index, IList list)
    {
        if (IsReadOnly || index < 0 || index >= list.Count)
        {
            return;
        }

        list.RemoveAt(index);
        OnValueChanged?.Invoke();
        OnAnimationsChanged?.Invoke();
        Rebuild();
    }

    private void UpdateCountLabel(int count)
    {
        if (_countLabel != null)
        {
            _countLabel.Text = count == 1 ? "1 animation" : $"{count} animations";
        }
    }

    private void UpdateAddButton()
    {
        if (_addButton != null)
        {
            _addButton.IsEnabled = !IsReadOnly;
        }
    }
}
