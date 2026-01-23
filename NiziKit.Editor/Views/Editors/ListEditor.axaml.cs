using System.Collections;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using NiziKit.Editor.Services;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views.Editors;

public partial class ListEditor : UserControl
{
    private TextBlock? _countLabel;
    private Button? _addButton;
    private ItemsControl? _itemsControl;

    public object? Instance { get; set; }
    public PropertyInfo? Property { get; set; }
    public AssetBrowserService? AssetBrowser { get; set; }
    public EditorViewModel? EditorViewModel { get; set; }
    public Action? OnValueChanged { get; set; }
    public bool IsReadOnly { get; set; }
    public Func<object>? ItemFactory { get; set; }
    public Func<object, int, PropertyEditorContext, Control?>? CustomItemEditorFactory { get; set; }

    public ListEditor()
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
        var elementType = GetElementType();

        for (var i = 0; i < list.Count; i++)
        {
            var item = list[i];
            var index = i;

            var itemPanel = CreateItemPanel(item, index, elementType, list);
            controls.Add(itemPanel);
        }

        _itemsControl.ItemsSource = controls;
    }

    private Control CreateItemPanel(object? item, int index, Type elementType, IList list)
    {
        var panel = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto,Auto"),
            Margin = new Thickness(0, 2)
        };

        var editorContainer = new Border
        {
            Margin = new Thickness(0, 0, 4, 0)
        };

        if (item != null)
        {
            var editor = CreateItemEditor(item, index, elementType);
            if (editor != null)
            {
                editorContainer.Child = editor;
            }
        }
        else
        {
            editorContainer.Child = new TextBlock
            {
                Text = "(null)",
                Classes = { "muted" }
            };
        }

        Grid.SetColumn(editorContainer, 0);
        panel.Children.Add(editorContainer);

        if (!IsReadOnly)
        {
            var upButton = new Button
            {
                Content = "^",
                Padding = new Thickness(6, 2),
                IsEnabled = index > 0
            };
            upButton.Click += (s, e) => MoveItem(index, index - 1, list);
            Grid.SetColumn(upButton, 1);
            panel.Children.Add(upButton);

            var downButton = new Button
            {
                Content = "v",
                Padding = new Thickness(6, 2),
                Margin = new Thickness(2, 0, 0, 0),
                IsEnabled = index < list.Count - 1
            };
            downButton.Click += (s, e) => MoveItem(index, index + 1, list);
            Grid.SetColumn(downButton, 2);
            panel.Children.Add(downButton);

            var removeButton = new Button
            {
                Content = "X",
                Padding = new Thickness(6, 2),
                Margin = new Thickness(2, 0, 0, 0)
            };
            removeButton.Click += (s, e) => RemoveItem(index, list);
            Grid.SetColumn(removeButton, 3);
            panel.Children.Add(removeButton);
        }

        return panel;
    }

    private Control? CreateItemEditor(object item, int index, Type elementType)
    {
        if (CustomItemEditorFactory != null)
        {
            var context = new PropertyEditorContext
            {
                Instance = item,
                Property = null!,
                AssetBrowser = AssetBrowser,
                EditorViewModel = EditorViewModel,
                OnValueChanged = () =>
                {
                    OnValueChanged?.Invoke();
                    Rebuild();
                }
            };
            return CustomItemEditorFactory(item, index, context);
        }

        if (elementType.IsPrimitive || elementType == typeof(string) || elementType.IsEnum)
        {
            return CreateSimpleTypeEditor(item, index, elementType);
        }

        return CreateComplexTypeEditor(item, index, elementType);
    }

    private Control CreateSimpleTypeEditor(object item, int index, Type elementType)
    {
        var list = Property?.GetValue(Instance) as IList;
        if (list == null)
        {
            return new TextBlock { Text = item?.ToString() ?? "(null)" };
        }

        if (elementType == typeof(string))
        {
            var textBox = new TextBox { Text = item?.ToString() ?? "" };
            textBox.LostFocus += (s, e) =>
            {
                list[index] = textBox.Text;
                OnValueChanged?.Invoke();
            };
            return textBox;
        }

        if (elementType == typeof(int))
        {
            var textBox = new TextBox { Text = item?.ToString() ?? "0" };
            textBox.LostFocus += (s, e) =>
            {
                if (int.TryParse(textBox.Text, out var value))
                {
                    list[index] = value;
                    OnValueChanged?.Invoke();
                }
            };
            return textBox;
        }

        if (elementType == typeof(float))
        {
            var editor = new DraggableValueEditor
            {
                Value = (float)(item ?? 0f),
                Label = "",
                DragSensitivity = 0.01f
            };
            editor.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == nameof(DraggableValueEditor.Value))
                {
                    list[index] = editor.Value;
                    OnValueChanged?.Invoke();
                }
            };
            return editor;
        }

        if (elementType.IsEnum)
        {
            var comboBox = new ComboBox
            {
                ItemsSource = Enum.GetValues(elementType),
                SelectedItem = item,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            comboBox.SelectionChanged += (s, e) =>
            {
                if (comboBox.SelectedItem != null)
                {
                    list[index] = comboBox.SelectedItem;
                    OnValueChanged?.Invoke();
                }
            };
            return comboBox;
        }

        return new TextBlock { Text = item?.ToString() ?? "(null)" };
    }

    private Control CreateComplexTypeEditor(object item, int index, Type elementType)
    {
        var panel = new StackPanel { Spacing = 4 };

        var indexLabel = new TextBlock
        {
            Text = $"[{index}]",
            Classes = { "caption" },
            Margin = new Thickness(0, 0, 0, 4)
        };
        panel.Children.Add(indexLabel);

        foreach (var prop in elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || !prop.CanWrite)
            {
                continue;
            }

            if (prop.PropertyType.IsByRefLike)
            {
                continue;
            }

            var propPanel = new StackPanel { Spacing = 2, Margin = new Thickness(8, 0, 0, 4) };

            var propLabel = new TextBlock { Text = prop.Name, Classes = { "label" } };
            propPanel.Children.Add(propLabel);

            var context = new PropertyEditorContext
            {
                Instance = item,
                Property = prop,
                AssetBrowser = AssetBrowser,
                EditorViewModel = EditorViewModel,
                OnValueChanged = () =>
                {
                    OnValueChanged?.Invoke();
                }
            };

            var editor = PropertyEditorRegistry.CreateEditor(context);
            if (editor != null)
            {
                propPanel.Children.Add(editor);
                panel.Children.Add(propPanel);
            }
        }

        return new Border
        {
            Background = Avalonia.Media.Brushes.Transparent,
            BorderBrush = Avalonia.Media.Brushes.Gray,
            BorderThickness = new Thickness(1, 0, 0, 0),
            Padding = new Thickness(8, 4),
            Child = panel
        };
    }

    private void OnAddClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Instance == null || Property == null || IsReadOnly)
        {
            return;
        }

        var list = Property.GetValue(Instance) as IList;
        if (list == null)
        {
            return;
        }

        var elementType = GetElementType();
        object? newItem;

        if (ItemFactory != null)
        {
            newItem = ItemFactory();
        }
        else if (elementType == typeof(string))
        {
            newItem = "";
        }
        else if (elementType.IsValueType)
        {
            newItem = Activator.CreateInstance(elementType);
        }
        else
        {
            try
            {
                newItem = Activator.CreateInstance(elementType);
            }
            catch
            {
                newItem = null;
            }
        }

        list.Add(newItem);
        OnValueChanged?.Invoke();
        Rebuild();
    }

    private void RemoveItem(int index, IList list)
    {
        if (IsReadOnly || index < 0 || index >= list.Count)
        {
            return;
        }

        list.RemoveAt(index);
        OnValueChanged?.Invoke();
        Rebuild();
    }

    private void MoveItem(int fromIndex, int toIndex, IList list)
    {
        if (IsReadOnly || fromIndex < 0 || fromIndex >= list.Count || toIndex < 0 || toIndex >= list.Count)
        {
            return;
        }

        var item = list[fromIndex];
        list.RemoveAt(fromIndex);
        list.Insert(toIndex, item);
        OnValueChanged?.Invoke();
        Rebuild();
    }

    private Type GetElementType()
    {
        if (Property == null)
        {
            return typeof(object);
        }

        var propType = Property.PropertyType;

        // Handle List<T>
        if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(List<>))
        {
            return propType.GetGenericArguments()[0];
        }

        // Handle IList<T>
        var listInterface = propType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
        if (listInterface != null)
        {
            return listInterface.GetGenericArguments()[0];
        }

        // Handle arrays
        if (propType.IsArray)
        {
            return propType.GetElementType() ?? typeof(object);
        }

        return typeof(object);
    }

    private void UpdateCountLabel(int count)
    {
        if (_countLabel != null)
        {
            _countLabel.Text = count == 1 ? "1 item" : $"{count} items";
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
