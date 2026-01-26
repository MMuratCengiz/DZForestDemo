using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using NiziKit.Assets.Serde;
using NiziKit.Components;
using NiziKit.Editor.Services;
using NiziKit.Editor.ViewModels;

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

    public JsonNode? CreateDefaultForProperty(string propertyPath, string propertyName)
    {
        var parentSchema = NavigateToPath(propertyPath);
        if (parentSchema?["properties"] is JsonObject props && props[propertyName] is JsonObject propDef)
        {
            var resolvedSchema = ResolveSchemaRef(propDef);
            return CreateDefaultFromSchema(resolvedSchema);
        }
        return null;
    }

    public JsonObject? GetPropertySchema(string propertyPath)
    {
        return NavigateToPath(propertyPath);
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

        var tokens = TokenizePath(propertyPath);
        var current = _schemaRoot;

        foreach (var token in tokens)
        {
            if (current == null)
            {
                return null;
            }

            if (token.StartsWith("[") && token.EndsWith("]"))
            {
                current = GetArrayItemsSchema(current);
                continue;
            }

            var props = current["properties"] as JsonObject;
            if (props != null && props[token] is JsonObject propDef)
            {
                current = ResolveSchemaRef(propDef);
                continue;
            }

            return null;
        }

        return current;
    }

    private static List<string> TokenizePath(string path)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();

        for (var i = 0; i < path.Length; i++)
        {
            var c = path[i];
            if (c == '.')
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else if (c == '[')
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
                var endBracket = path.IndexOf(']', i);
                if (endBracket > i)
                {
                    tokens.Add(path.Substring(i, endBracket - i + 1));
                    i = endBracket;
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    private JsonObject? GetArrayItemsSchema(JsonObject arraySchema)
    {
        if (arraySchema["items"] is JsonObject itemsDef)
        {
            return ResolveSchemaRef(itemsDef);
        }
        return null;
    }

    private JsonObject? ResolveSchemaRef(JsonObject schema)
    {
        if (schema["$ref"] is JsonValue refVal && refVal.TryGetValue<string>(out var refPath))
        {
            return ResolveRef(refPath);
        }
        return schema;
    }

    private string[]? FindEnumForPath(string propertyPath)
    {
        var propDef = NavigateToPath(propertyPath);
        if (propDef?["enum"] is JsonArray enumArr)
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

    public bool HasAdditionalProperties(string propertyPath)
    {
        var propDef = NavigateToPath(propertyPath);
        if (propDef == null)
        {
            return false;
        }

        if (propDef["additionalProperties"] is JsonObject)
        {
            return true;
        }

        if (propDef["additionalProperties"] is JsonValue val && val.TryGetValue<bool>(out var boolVal))
        {
            return boolVal;
        }

        return false;
    }

    public string? GetAdditionalPropertiesType(string propertyPath)
    {
        var propDef = NavigateToPath(propertyPath);
        if (propDef?["additionalProperties"] is JsonObject addProps)
        {
            if (addProps["type"] is JsonValue typeVal && typeVal.TryGetValue<string>(out var typeStr))
            {
                return typeStr;
            }
        }

        return null;
    }

    public string? GetAssetRefType(string propertyPath)
    {
        var propDef = NavigateToPath(propertyPath);
        if (propDef?["$assetRef"] is JsonValue assetRefVal && assetRefVal.TryGetValue<string>(out var assetRefType))
        {
            return assetRefType;
        }
        return null;
    }

    public bool ShouldShowEmpty(string propertyPath)
    {
        var propDef = NavigateToPath(propertyPath);
        if (propDef?["$showEmpty"] is JsonValue showEmptyVal && showEmptyVal.TryGetValue<bool>(out var showEmpty))
        {
            return showEmpty;
        }
        return false;
    }

    public JsonObject? GetArrayItemSchema(string propertyPath)
    {
        var propDef = NavigateToPath(propertyPath);
        if (propDef == null)
        {
            return null;
        }

        if (propDef["items"] is JsonObject itemsDef)
        {
            if (itemsDef["$ref"] is JsonValue refVal && refVal.TryGetValue<string>(out var refPath))
            {
                return ResolveRef(refPath);
            }
            return itemsDef;
        }

        return null;
    }

    public JsonNode? CreateDefaultFromSchema(JsonObject? schema)
    {
        if (schema == null)
        {
            return null;
        }

        if (schema["type"] is JsonValue typeVal && typeVal.TryGetValue<string>(out var typeStr))
        {
            switch (typeStr)
            {
                case "string":
                    if (schema["default"] is JsonValue defStr && defStr.TryGetValue<string>(out var defaultStr))
                    {
                        return defaultStr;
                    }
                    if (schema["enum"] is JsonArray enumArr && enumArr.Count > 0)
                    {
                        return enumArr[0]?.ToString() ?? "";
                    }
                    return "";
                case "integer":
                    if (schema["default"] is JsonValue defInt && defInt.TryGetValue<int>(out var defaultInt))
                    {
                        return defaultInt;
                    }
                    return 0;
                case "number":
                    if (schema["default"] is JsonValue defNum && defNum.TryGetValue<double>(out var defaultNum))
                    {
                        return defaultNum;
                    }
                    return 0.0;
                case "boolean":
                    if (schema["default"] is JsonValue defBool && defBool.TryGetValue<bool>(out var defaultBool))
                    {
                        return defaultBool;
                    }
                    return false;
                case "array":
                    return new JsonArray();
                case "object":
                    return CreateDefaultObject(schema);
            }
        }

        if (schema["properties"] is JsonObject)
        {
            return CreateDefaultObject(schema);
        }

        return null;
    }

    private JsonObject CreateDefaultObject(JsonObject schema)
    {
        var result = new JsonObject();

        if (schema["properties"] is JsonObject props)
        {
            var required = new HashSet<string>();
            if (schema["required"] is JsonArray reqArr)
            {
                foreach (var req in reqArr)
                {
                    if (req?.ToString() is string reqStr)
                    {
                        required.Add(reqStr);
                    }
                }
            }

            foreach (var kvp in props)
            {
                if (!required.Contains(kvp.Key))
                {
                    continue;
                }

                JsonObject? propSchema = null;
                if (kvp.Value is JsonObject propDef)
                {
                    if (propDef["$ref"] is JsonValue refVal && refVal.TryGetValue<string>(out var refPath))
                    {
                        propSchema = ResolveRef(refPath);
                    }
                    else
                    {
                        propSchema = propDef;
                    }
                }

                var defaultValue = CreateDefaultFromSchema(propSchema);
                if (defaultValue != null)
                {
                    result[kvp.Key] = defaultValue;
                }
            }
        }

        return result;
    }
}

public partial class JsonFormEditor : UserControl
{
    private JsonNode? _rootNode;
    private string? _originalJson;
    private string? _filePath;
    private ParsedJsonSchema? _schema;

    private static readonly Dictionary<string, ParsedJsonSchema> SchemaCache = new();

    public EditorViewModel? EditorViewModel { get; set; }
    public AssetBrowserService? AssetBrowser { get; set; }

    private static double GetIconSize(string key, double fallback = 12)
    {
        if (Avalonia.Application.Current?.TryFindResource(key, out var resource) == true && resource is double size)
            return size;
        return fallback;
    }

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
        if (rootObj["$schema"] is JsonValue schemaVal && schemaVal.TryGetValue<string>(out var schemaPath))
        {
            if (_filePath != null)
            {
                var fileDir = Path.GetDirectoryName(_filePath);
                if (fileDir != null)
                {
                    var schemaFullPath = Path.GetFullPath(Path.Combine(fileDir, schemaPath));

                    if (SchemaCache.TryGetValue(schemaFullPath, out var cached))
                    {
                        _schema = cached;
                        return;
                    }

                    if (File.Exists(schemaFullPath))
                    {
                        try
                        {
                            var schemaJson = File.ReadAllText(schemaFullPath);
                            _schema = new ParsedJsonSchema(schemaJson);
                            SchemaCache[schemaFullPath] = _schema;
                            return;
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        if (_filePath == null)
        {
            return;
        }

        var embeddedSchema = JsonSchemaRegistry.GetSchemaForFile(_filePath);
        if (embeddedSchema != null)
        {
            var cacheKey = $"embedded:{_filePath}";
            if (SchemaCache.TryGetValue(cacheKey, out var cached))
            {
                _schema = cached;
                return;
            }

            _schema = new ParsedJsonSchema(embeddedSchema);
            SchemaCache[cacheKey] = _schema;
        }
    }

    public string GetJson()
    {
        if (_rootNode == null)
        {
            return _originalJson ?? "{}";
        }

        return _rootNode.ToJsonString(NiziJsonSerializationOptions.Default);
    }

    private void BuildFormFromJsonNode(JsonObject obj, Panel container, string pathPrefix)
    {
        var existingKeys = obj.Select(kvp => kvp.Key).ToHashSet();

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
                if (_schema?.HasAdditionalProperties(propertyPath) == true)
                {
                    var section = CreateDictionarySection(kvp.Key, displayName, childObj, obj, propertyPath);
                    container.Children.Add(section);
                }
                else
                {
                    var section = CreateObjectSection(kvp.Key, displayName, childObj, propertyPath);
                    container.Children.Add(section);
                }
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

        var schemaProps = _schema?.GetObjectProperties(pathPrefix);
        if (schemaProps != null)
        {
            foreach (var prop in schemaProps)
            {
                if (existingKeys.Contains(prop) || prop == "$schema")
                {
                    continue;
                }

                var propertyPath = string.IsNullOrEmpty(pathPrefix) ? prop : $"{pathPrefix}.{prop}";
                if (_schema?.ShouldShowEmpty(propertyPath) == true)
                {
                    var emptyObj = new JsonObject();
                    obj[prop] = emptyObj;
                    var displayName = FormatPropertyName(prop);
                    var section = CreateObjectSection(prop, displayName, emptyObj, propertyPath);
                    container.Children.Add(section);
                }
            }

            var missingProps = schemaProps.Where(p => !existingKeys.Contains(p) && p != "$schema" && _schema?.ShouldShowEmpty(string.IsNullOrEmpty(pathPrefix) ? p : $"{pathPrefix}.{p}") != true).ToList();
            if (missingProps.Count > 0)
            {
                var addPropRow = CreateAddPropertyRow(obj, pathPrefix, missingProps);
                container.Children.Add(addPropRow);
            }
        }
    }

    private void BuildFormFromJsonNodeWithSchema(JsonObject obj, Panel container, string pathPrefix)
    {
        var schemaProps = _schema?.GetObjectProperties(pathPrefix);
        var existingKeys = obj.Select(kvp => kvp.Key).ToHashSet();

        if (schemaProps != null && schemaProps.Length > 0)
        {
            foreach (var prop in schemaProps)
            {
                if (prop == "$schema")
                {
                    continue;
                }

                var displayName = FormatPropertyName(prop);
                var propertyPath = string.IsNullOrEmpty(pathPrefix) ? prop : $"{pathPrefix}.{prop}";
                var value = obj[prop];

                if (value is JsonObject childObj)
                {
                    if (_schema?.HasAdditionalProperties(propertyPath) == true)
                    {
                        var section = CreateDictionarySection(prop, displayName, childObj, obj, propertyPath);
                        container.Children.Add(section);
                    }
                    else
                    {
                        var section = CreateObjectSection(prop, displayName, childObj, propertyPath);
                        container.Children.Add(section);
                    }
                }
                else if (value is JsonArray arr)
                {
                    var section = CreateArraySection(prop, displayName, arr, obj, propertyPath);
                    container.Children.Add(section);
                }
                else if (value != null)
                {
                    var row = CreatePropertyRow(displayName, CreateEditorForValue(prop, value, obj, propertyPath));
                    container.Children.Add(row);
                }
            }

            var missingProps = schemaProps.Where(p => !existingKeys.Contains(p)).ToList();
            if (missingProps.Count > 0)
            {
                var addPropRow = CreateAddPropertyRow(obj, pathPrefix, missingProps);
                container.Children.Add(addPropRow);
            }
        }
        else
        {
            BuildFormFromJsonNode(obj, container, pathPrefix);
        }
    }

    private Control CreateObjectSection(string key, string displayName, JsonObject obj, string propertyPath)
    {
        var expander = new Expander
        {
            Header = displayName,
            IsExpanded = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 4),
            ContentTransition = null
        };

        var nestedPanel = new StackPanel
        {
            Spacing = 4,
            Margin = new Thickness(0, 4)
        };

        var schemaProps = _schema?.GetObjectProperties(propertyPath);
        var existingKeys = obj.Select(kvp => kvp.Key).ToHashSet();

        if (schemaProps != null && schemaProps.Length > 0)
        {
            foreach (var prop in schemaProps)
            {
                var propDisplayName = FormatPropertyName(prop);
                var propPath = $"{propertyPath}.{prop}";
                var value = obj[prop];

                if (value is JsonObject childObj)
                {
                    if (_schema?.HasAdditionalProperties(propPath) == true)
                    {
                        var section = CreateDictionarySection(prop, propDisplayName, childObj, obj, propPath);
                        nestedPanel.Children.Add(section);
                    }
                    else
                    {
                        var section = CreateObjectSection(prop, propDisplayName, childObj, propPath);
                        nestedPanel.Children.Add(section);
                    }
                }
                else if (value is JsonArray arr)
                {
                    var section = CreateArraySection(prop, propDisplayName, arr, obj, propPath);
                    nestedPanel.Children.Add(section);
                }
                else if (value != null)
                {
                    var editor = CreateEditorForValue(prop, value, obj, propPath);
                    var row = CreatePropertyRow(propDisplayName, editor);
                    nestedPanel.Children.Add(row);
                }
            }

            var missingProps = schemaProps.Where(p => !existingKeys.Contains(p)).ToList();
            if (missingProps.Count > 0)
            {
                var addPropRow = CreateAddPropertyRow(obj, propertyPath, missingProps);
                nestedPanel.Children.Add(addPropRow);
            }
        }
        else
        {
            BuildFormFromJsonNode(obj, nestedPanel, propertyPath);
        }

        expander.Content = nestedPanel;
        return expander;
    }

    private Control CreateAddPropertyRow(JsonObject obj, string propertyPath, List<string> missingProps)
    {
        var button = new Button
        {
            Content = "Add Property...",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var menuItems = new List<MenuItem>();
        var flyout = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedLeft };
        for (var i = 0; i < missingProps.Count; i++)
        {
            var propName = missingProps[i];
            var displayName = FormatPropertyName(propName);
            var item = new MenuItem { Header = displayName };
            item.Click += (s, e) =>
            {
                var defaultValue = _schema?.CreateDefaultForProperty(propertyPath, propName);
                obj[propName] = defaultValue ?? JsonValue.Create("");
                ValueChanged?.Invoke();
                RefreshForm();
            };
            flyout.Items.Add(item);
            menuItems.Add(item);
        }

        flyout.Opened += (s, e) =>
        {
            var width = button.Bounds.Width;
            foreach (var item in menuItems)
            {
                item.MinWidth = width;
            }
        };

        button.Flyout = flyout;

        return button;
    }

    private Control CreateDictionarySection(string key, string displayName, JsonObject dictObj, JsonObject parent, string propertyPath)
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
            Content = new SymbolIcon { Symbol = Symbol.Add, FontSize = GetIconSize("IconSizeSmall") },
            Padding = new Thickness(4)
        };
        ToolTip.SetTip(addButton, "Add Entry");
        addButton.Click += (s, e) =>
        {
            var newKey = GenerateUniqueKey(dictObj, "NEW_KEY");
            dictObj[newKey] = "";
            ValueChanged?.Invoke();
            RefreshForm();
        };
        Grid.SetColumn(addButton, 1);
        headerGrid.Children.Add(addButton);

        container.Children.Add(headerGrid);

        var itemsPanel = new StackPanel { Spacing = 4 };

        foreach (var kvp in dictObj.ToList())
        {
            var entryContainer = CreateDictionaryEntryEditor(dictObj, kvp.Key, kvp.Value);
            itemsPanel.Children.Add(entryContainer);
        }

        container.Children.Add(itemsPanel);

        return container;
    }

    private Control CreateDictionaryEntryEditor(JsonObject dictObj, string entryKey, JsonNode? entryValue)
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
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
        };

        var keyTextBox = new TextBox
        {
            Text = entryKey,
            Width = 120,
            Margin = new Thickness(0, 0, 8, 0),
            Watermark = "Key"
        };

        var currentKey = entryKey;
        keyTextBox.LostFocus += (s, e) =>
        {
            var newKey = keyTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(newKey) || newKey == currentKey)
            {
                keyTextBox.Text = currentKey;
                return;
            }

            if (dictObj.ContainsKey(newKey))
            {
                keyTextBox.Text = currentKey;
                return;
            }

            var value = dictObj[currentKey];
            dictObj.Remove(currentKey);
            dictObj[newKey] = value?.DeepClone();
            currentKey = newKey;
            ValueChanged?.Invoke();
        };
        Grid.SetColumn(keyTextBox, 0);
        grid.Children.Add(keyTextBox);

        var valueTextBox = new TextBox
        {
            Text = entryValue?.ToString() ?? "",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Watermark = "Value"
        };
        valueTextBox.LostFocus += (s, e) =>
        {
            dictObj[currentKey] = valueTextBox.Text ?? "";
            ValueChanged?.Invoke();
        };
        Grid.SetColumn(valueTextBox, 1);
        grid.Children.Add(valueTextBox);

        var removeButton = new Button
        {
            Content = new SymbolIcon { Symbol = Symbol.Remove, FontSize = GetIconSize("IconSizeSmall") },
            Padding = new Thickness(4),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        ToolTip.SetTip(removeButton, "Remove");
        removeButton.Click += (s, e) =>
        {
            dictObj.Remove(currentKey);
            ValueChanged?.Invoke();
            RefreshForm();
        };
        Grid.SetColumn(removeButton, 2);
        grid.Children.Add(removeButton);

        border.Child = grid;
        return border;
    }

    private static string GenerateUniqueKey(JsonObject obj, string baseKey)
    {
        if (!obj.ContainsKey(baseKey))
        {
            return baseKey;
        }

        var counter = 1;
        while (obj.ContainsKey($"{baseKey}_{counter}"))
        {
            counter++;
        }
        return $"{baseKey}_{counter}";
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
            Content = new SymbolIcon { Symbol = Symbol.Add, FontSize = GetIconSize("IconSizeSmall") },
            Padding = new Thickness(4)
        };
        ToolTip.SetTip(addButton, "Add");
        addButton.Click += (s, e) =>
        {
            AddArrayItem(arr, propertyPath);
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
            var headerText = GetArrayItemHeader(itemObj, index);

            var expander = new Expander
            {
                Header = headerText,
                IsExpanded = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ContentTransition = null
            };

            var panel = new StackPanel { Spacing = 4 };
            BuildFormFromJsonNodeWithSchema(itemObj, panel, propertyPath);
            expander.Content = panel;
            content = expander;
        }
        else
        {
            content = CreatePrimitiveArrayItemEditor(arr, index, item);
        }

        Grid.SetColumn(content, 0);
        grid.Children.Add(content);

        var removeButton = new Button
        {
            Content = new SymbolIcon { Symbol = Symbol.Remove, FontSize = GetIconSize("IconSizeSmall") },
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

    private static string GetArrayItemHeader(JsonObject itemObj, int index)
    {
        string? identifier = null;

        if (itemObj["name"] is JsonValue nameVal && nameVal.TryGetValue<string>(out var name) && !string.IsNullOrEmpty(name))
        {
            identifier = name;
        }
        else if (itemObj["stage"] is JsonValue stageVal && stageVal.TryGetValue<string>(out var stage) && !string.IsNullOrEmpty(stage))
        {
            identifier = stage;
        }
        else if (itemObj["type"] is JsonValue typeVal && typeVal.TryGetValue<string>(out var type) && !string.IsNullOrEmpty(type))
        {
            identifier = type;
        }

        return identifier != null ? $"[{index}] {identifier}" : $"Item {index}";
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

    private void AddArrayItem(JsonArray arr, string propertyPath)
    {
        var itemSchema = _schema?.GetArrayItemSchema(propertyPath);
        if (itemSchema != null)
        {
            var newItem = _schema?.CreateDefaultFromSchema(itemSchema);
            if (newItem != null)
            {
                arr.Add(newItem);
                ValueChanged?.Invoke();
                return;
            }
        }

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
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            Margin = new Thickness(0, 2)
        };

        var labelBlock = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            MinWidth = 120,
            Margin = new Thickness(0, 0, 12, 0)
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

                var assetRefType = _schema?.GetAssetRefType(propertyPath);
                if (assetRefType != null)
                {
                    return CreateAssetRefEditor(key, strVal, parent, assetRefType);
                }

                return CreateStringEditor(key, strVal, parent);
            }
        }

        var enumVals = _schema?.GetEnumValues(propertyPath);
        if (enumVals != null && enumVals.Length > 0)
        {
            return CreateEnumEditor(key, value?.ToString(), parent, enumVals);
        }

        var assetRef = _schema?.GetAssetRefType(propertyPath);
        if (assetRef != null)
        {
            return CreateAssetRefEditor(key, value?.ToString() ?? "", parent, assetRef);
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

    private Control CreateAssetRefEditor(string key, string? value, JsonObject parent, string assetRefTypeName)
    {
        var assetType = assetRefTypeName switch
        {
            "Texture" => AssetRefType.Texture,
            "Mesh" => AssetRefType.Mesh,
            "Material" => AssetRefType.Material,
            "Skeleton" => AssetRefType.Skeleton,
            "Animation" => AssetRefType.Animation,
            "Shader" => AssetRefType.Shader,
            _ => AssetRefType.Texture
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var textBox = new TextBox
        {
            Text = value ?? "",
            Watermark = $"(Select {assetRefTypeName})",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            IsReadOnly = true
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

        Grid.SetColumn(textBox, 0);
        grid.Children.Add(textBox);

        var browseButton = new Button
        {
            Content = new SymbolIcon { Symbol = Symbol.OpenFolder, FontSize = GetIconSize("IconSizeMedium") },
            Padding = new Thickness(8, 6),
            Margin = new Thickness(4, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Stretch
        };
        ToolTip.SetTip(browseButton, $"Browse {assetRefTypeName}s");

        browseButton.Click += (s, e) =>
        {
            if (EditorViewModel == null || AssetBrowser == null)
            {
                return;
            }

            EditorViewModel.OpenAssetPicker(assetType, value, asset =>
            {
                if (asset != null)
                {
                    textBox.Text = asset.FullReference;
                    parent[key] = asset.FullReference;
                    ValueChanged?.Invoke();
                }
            });
        };

        Grid.SetColumn(browseButton, 1);
        grid.Children.Add(browseButton);

        return grid;
    }

    private Control CreateEnumEditor(string key, string? value, JsonObject parent, string[] enumValues)
    {
        var button = new Button
        {
            Content = value ?? enumValues.FirstOrDefault() ?? "",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left
        };

        var menuItems = new List<MenuItem>();
        var flyout = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedLeft };
        foreach (var enumValue in enumValues)
        {
            var item = new MenuItem { Header = enumValue };
            item.Click += (s, e) =>
            {
                parent[key] = enumValue;
                button.Content = enumValue;
                ValueChanged?.Invoke();
            };
            flyout.Items.Add(item);
            menuItems.Add(item);
        }

        flyout.Opened += (s, e) =>
        {
            var width = button.Bounds.Width;
            foreach (var item in menuItems)
            {
                item.MinWidth = width;
            }
        };

        button.Flyout = flyout;

        return button;
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
            Dispatcher.UIThread.Invoke(RefreshForm);
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
