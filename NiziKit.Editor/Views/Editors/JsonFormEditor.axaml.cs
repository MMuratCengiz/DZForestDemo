using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;

namespace NiziKit.Editor.Views.Editors;

public class JsonAssetSchema
{
    public required string Name { get; init; }
    public required string FileExtension { get; init; }

    public static JsonAssetSchema? GetSchemaForFile(string filePath)
    {
        return null;
    }
}

public class ParsedJsonSchema
{
    private readonly JsonObject? _schemaRoot;
    private readonly Dictionary<string, string[]> _enumCache = new();

    public ParsedJsonSchema(string schemaJson)
    {
        try
        {
            _schemaRoot = JsonNode.Parse(schemaJson) as JsonObject;
        }
        catch
        {
            _schemaRoot = null;
        }
    }

    public string[]? GetEnumValues(string propertyPath)
    {
        if (_schemaRoot == null)
        {
            return null;
        }

        if (_enumCache.TryGetValue(propertyPath, out var cached))
        {
            return cached;
        }

        var enumValues = FindEnumForPath(propertyPath);
        if (enumValues != null)
        {
            _enumCache[propertyPath] = enumValues;
        }

        return enumValues;
    }

    public string[]? GetObjectProperties(string propertyPath)
    {
        if (_schemaRoot == null)
        {
            return null;
        }

        var propDef = NavigateToPath(propertyPath);
        if (propDef?["properties"] is JsonObject propsObj)
        {
            return propsObj.Select(kvp => kvp.Key).ToArray();
        }

        return null;
    }

    private JsonObject? NavigateToPath(string propertyPath)
    {
        if (_schemaRoot == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(propertyPath))
        {
            return _schemaRoot;
        }

        var parts = propertyPath.Split('.');
        JsonObject? current = _schemaRoot;

        foreach (var part in parts)
        {
            if (current == null)
            {
                return null;
            }

            var props = current["properties"] as JsonObject;
            if (props != null && props[part] is JsonObject propDef)
            {
                if (propDef["$ref"] is JsonValue refVal && refVal.TryGetValue<string>(out var refPath))
                {
                    current = ResolveRef(refPath);
                }
                else
                {
                    current = propDef;
                }
                continue;
            }

            return null;
        }

        return current;
    }

    private string[]? FindEnumForPath(string propertyPath)
    {
        if (_schemaRoot == null)
        {
            return null;
        }

        var parts = propertyPath.Split('.');
        JsonObject? current = _schemaRoot;

        foreach (var part in parts)
        {
            if (current == null)
            {
                return null;
            }

            var props = current["properties"] as JsonObject;
            if (props != null && props[part] is JsonObject propDef)
            {
                if (propDef["$ref"] is JsonValue refVal && refVal.TryGetValue<string>(out var refPath))
                {
                    current = ResolveRef(refPath);
                }
                else
                {
                    current = propDef;
                }
                continue;
            }

            if (current["items"] is JsonObject itemsDef)
            {
                if (itemsDef["$ref"] is JsonValue refVal && refVal.TryGetValue<string>(out var refPath))
                {
                    current = ResolveRef(refPath);
                }
                else
                {
                    current = itemsDef;
                }

                var itemProps = current?["properties"] as JsonObject;
                if (itemProps != null && itemProps[part] is JsonObject itemPropDef)
                {
                    if (itemPropDef["$ref"] is JsonValue propRefVal && propRefVal.TryGetValue<string>(out var propRefPath))
                    {
                        current = ResolveRef(propRefPath);
                    }
                    else
                    {
                        current = itemPropDef;
                    }
                    continue;
                }
            }

            return null;
        }

        if (current?["enum"] is JsonArray enumArr)
        {
            return enumArr.Select(e => e?.ToString() ?? "").ToArray();
        }

        return null;
    }

    private JsonObject? ResolveRef(string refPath)
    {
        if (_schemaRoot == null || !refPath.StartsWith("#/"))
        {
            return null;
        }

        var parts = refPath[2..].Split('/');
        JsonNode? current = _schemaRoot;

        foreach (var part in parts)
        {
            if (current is JsonObject obj)
            {
                current = obj[part];
            }
            else
            {
                return null;
            }
        }

        return current as JsonObject;
    }
}

public partial class JsonFormEditor : UserControl
{
    private JsonNode? _rootNode;
    private string? _originalJson;
    private string? _filePath;
    private ParsedJsonSchema? _schema;

    private static readonly Dictionary<string, ParsedJsonSchema> SchemaCache = new();

    public JsonFormEditor()
    {
        InitializeComponent();
    }

    public event Action? ValueChanged;

    public void LoadJson(string json, JsonAssetSchema? schema = null, string? filePath = null)
    {
        FormContainer.Children.Clear();
        _rootNode = null;
        _originalJson = json;
        _filePath = filePath;
        _schema = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            _rootNode = JsonNode.Parse(json);
            if (_rootNode is JsonObject rootObj)
            {
                LoadSchemaFromJson(rootObj);
                BuildFormFromJsonNode(rootObj, FormContainer, "");
            }
        }
        catch (JsonException ex)
        {
            FormContainer.Children.Add(new TextBlock
            {
                Text = $"Invalid JSON: {ex.Message}"
            });
        }
    }

    private void LoadSchemaFromJson(JsonObject rootObj)
    {
        if (rootObj["$schema"] is not JsonValue schemaVal || !schemaVal.TryGetValue<string>(out var schemaPath))
        {
            return;
        }

        if (_filePath == null)
        {
            return;
        }

        var fileDir = Path.GetDirectoryName(_filePath);
        if (fileDir == null)
        {
            return;
        }

        var schemaFullPath = Path.GetFullPath(Path.Combine(fileDir, schemaPath));

        if (SchemaCache.TryGetValue(schemaFullPath, out var cached))
        {
            _schema = cached;
            return;
        }

        if (!File.Exists(schemaFullPath))
        {
            return;
        }

        try
        {
            var schemaJson = File.ReadAllText(schemaFullPath);
            _schema = new ParsedJsonSchema(schemaJson);
            SchemaCache[schemaFullPath] = _schema;
        }
        catch
        {
        }
    }

    public string GetJson()
    {
        if (_rootNode == null)
        {
            return _originalJson ?? "{}";
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        return _rootNode.ToJsonString(options);
    }

    private void BuildFormFromJsonNode(JsonObject obj, Panel container, string pathPrefix)
    {
        foreach (var kvp in obj)
        {
            if (kvp.Key == "$schema")
            {
                continue;
            }

            var displayName = FormatPropertyName(kvp.Key);
            var propertyPath = string.IsNullOrEmpty(pathPrefix) ? kvp.Key : $"{pathPrefix}.{kvp.Key}";

            if (kvp.Value is JsonObject childObj)
            {
                var section = CreateObjectSection(kvp.Key, displayName, childObj, propertyPath);
                container.Children.Add(section);
            }
            else if (kvp.Value is JsonArray arr)
            {
                var section = CreateArraySection(kvp.Key, displayName, arr, obj, propertyPath);
                container.Children.Add(section);
            }
            else
            {
                var row = CreatePropertyRow(displayName, CreateEditorForValue(kvp.Key, kvp.Value, obj, propertyPath));
                container.Children.Add(row);
            }
        }
    }

    private Control CreateObjectSection(string key, string displayName, JsonObject obj, string propertyPath)
    {
        var expander = new Expander
        {
            Header = displayName,
            IsExpanded = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 4)
        };

        var nestedPanel = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(8, 4)
        };

        var schemaProps = _schema?.GetObjectProperties(propertyPath);
        if (schemaProps != null && schemaProps.Length > 0)
        {
            foreach (var prop in schemaProps)
            {
                var propDisplayName = FormatPropertyName(prop);
                var propPath = $"{propertyPath}.{prop}";
                var value = obj[prop];

                if (value is JsonObject childObj)
                {
                    var section = CreateObjectSection(prop, propDisplayName, childObj, propPath);
                    nestedPanel.Children.Add(section);
                }
                else if (value is JsonArray arr)
                {
                    var section = CreateArraySection(prop, propDisplayName, arr, obj, propPath);
                    nestedPanel.Children.Add(section);
                }
                else
                {
                    var editor = CreateEditorForValue(prop, value, obj, propPath);
                    var row = CreatePropertyRow(propDisplayName, editor);
                    nestedPanel.Children.Add(row);
                }
            }
        }
        else
        {
            BuildFormFromJsonNode(obj, nestedPanel, propertyPath);
        }

        expander.Content = nestedPanel;
        return expander;
    }

    private Control CreateArraySection(string key, string displayName, JsonArray arr, JsonObject parent, string propertyPath)
    {
        var container = new StackPanel { Spacing = 4, Margin = new Thickness(0, 4) };

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(0, 4, 0, 4)
        };

        var headerText = new TextBlock
        {
            Text = displayName,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(headerText, 0);
        headerGrid.Children.Add(headerText);

        var addButton = new Button
        {
            Content = new SymbolIcon { Symbol = Symbol.Add, FontSize = 12 },
            Padding = new Thickness(4)
        };
        ToolTip.SetTip(addButton, "Add");
        addButton.Click += (s, e) =>
        {
            AddArrayItem(arr, key);
            RefreshForm();
        };
        Grid.SetColumn(addButton, 1);
        headerGrid.Children.Add(addButton);

        container.Children.Add(headerGrid);

        var itemsPanel = new StackPanel { Spacing = 4 };

        for (var i = 0; i < arr.Count; i++)
        {
            var item = arr[i];
            var itemPath = $"{propertyPath}[{i}]";
            var itemContainer = CreateArrayItemEditor(arr, i, item, itemPath);
            itemsPanel.Children.Add(itemContainer);
        }

        container.Children.Add(itemsPanel);

        return container;
    }

    private Control CreateArrayItemEditor(JsonArray arr, int index, JsonNode? item, string propertyPath)
    {
        var border = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 2)
        };
        if (Avalonia.Application.Current?.TryFindResource("CardStrokeColorDefaultBrush", out var brush) == true)
        {
            border.BorderBrush = brush as Avalonia.Media.IBrush;
        }

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        Control content;

        if (item is JsonObject itemObj)
        {
            var panel = new StackPanel { Spacing = 4 };
            BuildFormFromJsonNode(itemObj, panel, propertyPath);
            content = panel;
        }
        else
        {
            content = CreatePrimitiveArrayItemEditor(arr, index, item);
        }

        Grid.SetColumn(content, 0);
        grid.Children.Add(content);

        var removeButton = new Button
        {
            Content = new SymbolIcon { Symbol = Symbol.Remove, FontSize = 12 },
            Padding = new Thickness(4),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(4, 0, 0, 0)
        };
        ToolTip.SetTip(removeButton, "Remove");
        removeButton.Click += (s, e) =>
        {
            arr.RemoveAt(index);
            ValueChanged?.Invoke();
            RefreshForm();
        };
        Grid.SetColumn(removeButton, 1);
        grid.Children.Add(removeButton);

        border.Child = grid;
        return border;
    }

    private Control CreatePrimitiveArrayItemEditor(JsonArray arr, int index, JsonNode? item)
    {
        var textBox = new TextBox
        {
            Text = item?.ToString() ?? "",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        textBox.LostFocus += (s, e) =>
        {
            UpdateArrayItem(arr, index, textBox.Text);
        };

        return textBox;
    }

    private void AddArrayItem(JsonArray arr, string key)
    {
        if (arr.Count > 0 && arr[0] is JsonObject existingObj)
        {
            var newObj = new JsonObject();
            foreach (var kvp in existingObj)
            {
                newObj[kvp.Key] = kvp.Value switch
                {
                    JsonValue val when val.TryGetValue<string>(out _) => "",
                    JsonValue val when val.TryGetValue<int>(out _) => 0,
                    JsonValue val when val.TryGetValue<double>(out _) => 0.0,
                    JsonValue val when val.TryGetValue<bool>(out _) => false,
                    JsonObject => new JsonObject(),
                    JsonArray => new JsonArray(),
                    _ => null
                };
            }
            arr.Add(newObj);
        }
        else
        {
            arr.Add("");
        }

        ValueChanged?.Invoke();
    }

    private void UpdateArrayItem(JsonArray arr, int index, string? value)
    {
        if (index < 0 || index >= arr.Count)
        {
            return;
        }

        var original = arr[index];
        if (original is JsonValue)
        {
            if (int.TryParse(value, out var intVal))
            {
                arr[index] = intVal;
            }
            else if (double.TryParse(value, out var doubleVal))
            {
                arr[index] = doubleVal;
            }
            else if (bool.TryParse(value, out var boolVal))
            {
                arr[index] = boolVal;
            }
            else
            {
                arr[index] = value;
            }
        }

        ValueChanged?.Invoke();
    }

    private Grid CreatePropertyRow(string label, Control editor)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("140,*"),
            Margin = new Thickness(0, 2)
        };

        var labelBlock = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center
        };

        Grid.SetColumn(labelBlock, 0);
        Grid.SetColumn(editor, 1);
        row.Children.Add(labelBlock);
        row.Children.Add(editor);

        return row;
    }

    private Control CreateEditorForValue(string key, JsonNode? value, JsonObject parent, string propertyPath)
    {
        if (value is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<bool>(out var boolVal))
            {
                return CreateBooleanEditor(key, boolVal, parent);
            }

            if (jsonValue.TryGetValue<int>(out var intVal))
            {
                return CreateNumberEditor(key, intVal, parent, isInt: true);
            }

            if (jsonValue.TryGetValue<double>(out var doubleVal))
            {
                return CreateNumberEditor(key, doubleVal, parent, isInt: false);
            }

            if (jsonValue.TryGetValue<string>(out var strVal))
            {
                var enumValues = _schema?.GetEnumValues(propertyPath);
                if (enumValues != null && enumValues.Length > 0)
                {
                    return CreateEnumEditor(key, strVal, parent, enumValues);
                }

                return CreateStringEditor(key, strVal, parent);
            }
        }

        var enumVals = _schema?.GetEnumValues(propertyPath);
        if (enumVals != null && enumVals.Length > 0)
        {
            return CreateEnumEditor(key, value?.ToString(), parent, enumVals);
        }

        return CreateStringEditor(key, value?.ToString() ?? "", parent);
    }

    private Control CreateStringEditor(string key, string? value, JsonObject parent)
    {
        var textBox = new TextBox
        {
            Text = value ?? "",
            Watermark = "(None)",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        textBox.LostFocus += (s, e) =>
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                parent.Remove(key);
            }
            else
            {
                parent[key] = textBox.Text;
            }
            ValueChanged?.Invoke();
        };

        return textBox;
    }

    private Control CreateEnumEditor(string key, string? value, JsonObject parent, string[] enumValues)
    {
        var comboBox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = enumValues
        };

        var index = Array.IndexOf(enumValues, value);
        if (index >= 0)
        {
            comboBox.SelectedIndex = index;
        }

        comboBox.SelectionChanged += (s, e) =>
        {
            if (comboBox.SelectedItem is string selected)
            {
                parent[key] = selected;
                ValueChanged?.Invoke();
            }
        };

        return comboBox;
    }

    private Control CreateNumberEditor(string key, double value, JsonObject parent, bool isInt)
    {
        var textBox = new TextBox
        {
            Text = isInt ? ((int)value).ToString() : value.ToString(),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        textBox.LostFocus += (s, e) =>
        {
            if (isInt && int.TryParse(textBox.Text, out var intValue))
            {
                parent[key] = intValue;
                ValueChanged?.Invoke();
            }
            else if (double.TryParse(textBox.Text, out var newValue))
            {
                parent[key] = newValue;
                ValueChanged?.Invoke();
            }
        };

        return textBox;
    }

    private Control CreateBooleanEditor(string key, bool value, JsonObject parent)
    {
        var checkBox = new CheckBox { IsChecked = value };

        checkBox.IsCheckedChanged += (s, e) =>
        {
            parent[key] = checkBox.IsChecked ?? false;
            ValueChanged?.Invoke();
        };

        return checkBox;
    }

    private void RefreshForm()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(RefreshForm);
            return;
        }

        if (_rootNode is JsonObject rootObj)
        {
            FormContainer.Children.Clear();
            BuildFormFromJsonNode(rootObj, FormContainer, "");
        }
    }

    private static string FormatPropertyName(string name)
    {
        var result = new System.Text.StringBuilder();
        var prevWasLower = false;

        foreach (var c in name)
        {
            if (c == '_')
            {
                result.Append(' ');
                prevWasLower = false;
            }
            else if (char.IsUpper(c) && prevWasLower)
            {
                result.Append(' ');
                result.Append(c);
                prevWasLower = false;
            }
            else
            {
                result.Append(result.Length == 0 || result[^1] == ' ' ? char.ToUpper(c) : c);
                prevWasLower = char.IsLower(c);
            }
        }

        return result.ToString();
    }
}
