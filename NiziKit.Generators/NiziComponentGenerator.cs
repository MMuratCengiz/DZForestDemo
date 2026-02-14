using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NiziKit.Generators;

[Generator]
public class NiziComponentGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsCandidateClass(s),
                transform: static (ctx, _) => GetSemanticTarget(ctx))
            .Where(static m => m is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    private static bool IsCandidateClass(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDecl
               && classDecl.Modifiers.Any(SyntaxKind.PartialKeyword);
    }

    private static ClassDeclarationSyntax? GetSemanticTarget(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;

        foreach (var attributeList in classDecl.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var name = attribute.Name.ToString();
                if (name is "NiziComponent" or "NiziComponentAttribute")
                {
                    return classDecl;
                }
            }
        }

        return null;
    }

    private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax?> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            return;
        }

        var distinctClasses = classes.Where(c => c is not null).Distinct().ToList();
        var componentInfos = new List<ComponentInfo>();

        foreach (var classDecl in distinctClasses)
        {
            if (classDecl is null)
            {
                continue;
            }

            var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);

            if (classSymbol is null)
            {
                continue;
            }

            var info = ExtractComponentInfo(classSymbol, classDecl, compilation);
            componentInfos.Add(info);

            var source = GeneratePartialClass(info, classDecl, compilation);
            context.AddSource($"{classSymbol.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        if (componentInfos.Any(c => c.GenerateFactory))
        {
            var assemblyName = compilation.AssemblyName ?? "Generated";
            var registrationSource = GenerateModuleInitializer(componentInfos, assemblyName);
            context.AddSource("ComponentRegistration.g.cs", SourceText.From(registrationSource, Encoding.UTF8));
        }

        if (componentInfos.Count > 0)
        {
            var schemaSource = GenerateComponentSchema(componentInfos);
            context.AddSource("ComponentSchema.g.cs", SourceText.From(schemaSource, Encoding.UTF8));
        }
    }

    private static ComponentInfo ExtractComponentInfo(INamedTypeSymbol classSymbol, ClassDeclarationSyntax classDecl, Compilation compilation)
    {
        var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;

        var attribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "NiziComponentAttribute");

        string? typeName = null;
        var generateFactory = true;

        if (attribute != null)
        {
            foreach (var namedArg in attribute.NamedArguments)
            {
                if (namedArg is { Key: "TypeName", Value.Value: string tn })
                {
                    typeName = tn;
                }
                else if (namedArg is { Key: "GenerateFactory", Value.Value: bool gf })
                {
                    generateFactory = gf;
                }
            }
        }

        if (string.IsNullOrEmpty(typeName))
        {
            typeName = namespaceName + "." + className;
        }

        var properties = new List<PropertyInfo>();
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
            {
                continue;
            }

            var jsonPropAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "JsonPropertyAttribute");
            var serializeFieldAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "SerializeFieldAttribute");
            var assetRefAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "AssetRefAttribute");

            if (jsonPropAttr != null || serializeFieldAttr != null || assetRefAttr != null)
            {
                var propInfo = new PropertyInfo
                {
                    Name = property.Name,
                    Type = property.Type.ToDisplayString(),
                    TypeSymbol = property.Type
                };

                if (jsonPropAttr is { ConstructorArguments.Length: > 0 })
                {
                    propInfo.JsonName = jsonPropAttr.ConstructorArguments[0].Value as string;
                }
                else if (serializeFieldAttr != null)
                {
                    foreach (var namedArg in serializeFieldAttr.NamedArguments)
                    {
                        if (namedArg is { Key: "Name", Value.Value: string name })
                        {
                            propInfo.JsonName = name;
                        }
                    }
                    propInfo.JsonName ??= char.ToLower(property.Name[0]) + property.Name.Substring(1);
                }

                if (assetRefAttr is { ConstructorArguments.Length: >= 2 })
                {
                    propInfo.AssetType = (int?)assetRefAttr.ConstructorArguments[0].Value;
                    propInfo.AssetJsonName = assetRefAttr.ConstructorArguments[1].Value as string;
                }

                properties.Add(propInfo);
            }
        }

        var partialProperties = classDecl.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(p => p.Modifiers.Any(SyntaxKind.PartialKeyword))
            .Select(p =>
            {
                var typeInfo = semanticModel.GetTypeInfo(p.Type);
                var displayFormat = new SymbolDisplayFormat(
                    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

                var baseType = typeInfo.Type?.ToDisplayString(displayFormat) ?? p.Type.ToString();
                var isNullable = p.Type is NullableTypeSyntax;
                var propType = isNullable && !baseType.EndsWith("?") ? baseType + "?" : baseType;

                return new PartialPropertyInfo
                {
                    Name = p.Identifier.Text,
                    Type = propType
                };
            })
            .ToList();

        return new ComponentInfo
        {
            Namespace = namespaceName,
            ClassName = className,
            TypeName = typeName!,
            GenerateFactory = generateFactory,
            Properties = properties,
            PartialProperties = partialProperties
        };
    }

    private static string GeneratePartialClass(ComponentInfo info, ClassDeclarationSyntax classDecl, Compilation compilation)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using NiziKit.Core;");
        sb.AppendLine("using NiziKit.Components;");
        sb.AppendLine();
        sb.AppendLine($"namespace {info.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"partial class {info.ClassName} : IComponent");
        sb.AppendLine("{");

        sb.AppendLine("    public GameObject? Owner { get; set; }");
        sb.AppendLine();

        foreach (var prop in info.PartialProperties)
        {
            var fieldName = $"__{char.ToLower(prop.Name[0])}{prop.Name.Substring(1)}";

            sb.AppendLine($"    private {prop.Type} {fieldName};");
            sb.AppendLine($"    public partial {prop.Type} {prop.Name}");
            sb.AppendLine("    {");
            sb.AppendLine($"        get => {fieldName};");
            sb.AppendLine("        set");
            sb.AppendLine("        {");
            sb.AppendLine($"            {fieldName} = value;");
            sb.AppendLine("            Owner?.NotifyComponentChanged(this);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateModuleInitializer(List<ComponentInfo> components, string assemblyName)
    {
        var sb = new StringBuilder();
        var safeAssemblyName = assemblyName.Replace(".", "_").Replace("-", "_");

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using NiziKit.Core;");
        sb.AppendLine("using NiziKit.Components;");
        sb.AppendLine();
        sb.AppendLine($"namespace {safeAssemblyName}.Generated;");
        sb.AppendLine();
        sb.AppendLine("internal static class ComponentRegistration");
        sb.AppendLine("{");
        sb.AppendLine("    [ModuleInitializer]");
        sb.AppendLine("    internal static void Initialize()");
        sb.AppendLine("    {");

        foreach (var comp in components.Where(c => c.GenerateFactory))
        {
            sb.AppendLine($"        ComponentRegistry.Register(new {comp.ClassName}Factory());");
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        foreach (var comp in components.Where(c => c.GenerateFactory))
        {
            GenerateFactoryClass(sb, comp);
        }

        sb.AppendLine("    private static System.Numerics.Vector3 ParseVector3(JsonElement element)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() >= 3)");
        sb.AppendLine("        {");
        sb.AppendLine("            var arr = element.EnumerateArray().ToArray();");
        sb.AppendLine("            return new System.Numerics.Vector3(arr[0].GetSingle(), arr[1].GetSingle(), arr[2].GetSingle());");
        sb.AppendLine("        }");
        sb.AppendLine("        return System.Numerics.Vector3.Zero;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static System.Numerics.Vector2 ParseVector2(JsonElement element)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() >= 2)");
        sb.AppendLine("        {");
        sb.AppendLine("            var arr = element.EnumerateArray().ToArray();");
        sb.AppendLine("            return new System.Numerics.Vector2(arr[0].GetSingle(), arr[1].GetSingle());");
        sb.AppendLine("        }");
        sb.AppendLine("        return System.Numerics.Vector2.Zero;");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateFactoryClass(StringBuilder sb, ComponentInfo comp)
    {
        sb.AppendLine($"    private sealed class {comp.ClassName}Factory : IComponentFactory");
        sb.AppendLine("    {");
        sb.AppendLine($"        public string TypeName => \"{comp.TypeName}\";");
        sb.AppendLine();
        sb.AppendLine("        public IComponent Create(IReadOnlyDictionary<string, JsonElement>? properties, IAssetResolver resolver)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var component = new {comp.Namespace}.{comp.ClassName}();");

        foreach (var prop in comp.Properties)
        {
            if (!string.IsNullOrEmpty(prop.AssetJsonName))
            {
                var resolverMethod = prop.AssetType switch
                {
                    0 => "ResolveMesh",
                    1 => "ResolveMaterial",
                    2 => "ResolveTexture",
                    3 => "ResolveShader",
                    4 => "ResolveSkeleton",
                    5 => "ResolveAnimation",
                    _ => null
                };

                if (resolverMethod != null)
                {
                    sb.AppendLine($"            if (properties != null && properties.TryGetValue(\"{prop.AssetJsonName}\", out var {prop.Name.ToLower()}Ref) && {prop.Name.ToLower()}Ref.ValueKind == JsonValueKind.String)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                var {prop.Name.ToLower()}RefStr = {prop.Name.ToLower()}Ref.GetString()!;");
                    if (prop.Type == "string" || prop.Type == "string?")
                    {
                        sb.AppendLine($"                component.{prop.Name} = {prop.Name.ToLower()}RefStr;");
                    }
                    else
                    {
                        sb.AppendLine($"                component.{prop.Name} = resolver.{resolverMethod}({prop.Name.ToLower()}RefStr);");
                    }
                    sb.AppendLine("            }");
                }
            }
            else if (!string.IsNullOrEmpty(prop.JsonName))
            {
                var varName = $"{prop.Name.ToLower()}Val";
                var getterMethod = GetJsonGetterMethod(prop.Type, varName);
                if (getterMethod != null)
                {
                    sb.AppendLine($"            if (properties != null && properties.TryGetValue(\"{prop.JsonName}\", out var {varName}))");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                component.{prop.Name} = {getterMethod};");
                    sb.AppendLine("            }");
                }
            }
        }

        sb.AppendLine("            return component;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static string? GetJsonGetterMethod(string typeName, string varName)
    {
        var isNullable = typeName.EndsWith("?");
        var baseType = isNullable ? typeName.TrimEnd('?') : typeName;
        baseType = baseType.Replace("global::", "");
        var simpleType = baseType.Contains(".") ? baseType.Substring(baseType.LastIndexOf('.') + 1) : baseType;

        // Handle List<T> types
        if (baseType.StartsWith("System.Collections.Generic.List<") || baseType.StartsWith("List<"))
        {
            return $"System.Text.Json.JsonSerializer.Deserialize<{baseType}>({varName}.GetRawText())";
        }

        return simpleType switch
        {
            "String" or "string" => $"{varName}.GetString()",
            "Int32" or "int" => $"{varName}.GetInt32()",
            "Int64" or "long" => $"{varName}.GetInt64()",
            "Single" or "float" => $"{varName}.GetSingle()",
            "Double" or "double" => $"{varName}.GetDouble()",
            "Boolean" or "bool" => $"{varName}.GetBoolean()",
            "UInt32" or "uint" => $"{varName}.GetUInt32()",
            "Vector3" => $"ParseVector3({varName})",
            "Vector2" => $"ParseVector2({varName})",
            "PhysicsBodyType" => $"System.Enum.Parse<NiziKit.Physics.PhysicsBodyType>({varName}.GetString()!, true)",
            "PhysicsShapeType" => $"System.Enum.Parse<NiziKit.Physics.PhysicsShapeType>({varName}.GetString()!, true)",
            _ => null
        };
    }

    private static string GenerateComponentSchema(List<ComponentInfo> components)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace NiziKit.Generated;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated JSON Schema definitions for NiziKit components.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class ComponentSchema");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// JSON Schema $defs for all registered component types.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public const string ComponentDefs = \"\"\"");
        sb.AppendLine("  {");
        sb.AppendLine("    \"component\": {");
        sb.AppendLine("      \"oneOf\": [");
        for (var i = 0; i < components.Count; i++)
        {
            var comp = components[i];
            var comma = i < components.Count - 1 ? "," : "";
            sb.AppendLine($"        {{ \"$ref\": \"#/$defs/{comp.TypeName}\" }}{comma}");
        }
        sb.AppendLine("      ]");
        sb.AppendLine("    },");

        for (var i = 0; i < components.Count; i++)
        {
            var comp = components[i];
            var isLast = i == components.Count - 1;
            GenerateComponentSchemaDef(sb, comp, isLast);
        }

        sb.AppendLine("  }");
        sb.AppendLine("\"\"\";");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets all registered component type names.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static readonly string[] ComponentTypes = [");
        foreach (var comp in components)
        {
            sb.AppendLine($"        \"{comp.TypeName}\",");
        }
        sb.AppendLine("    ];");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateComponentSchemaDef(StringBuilder sb, ComponentInfo comp, bool isLast)
    {
        var comma = isLast ? "" : ",";

        var allProps = new List<(string jsonName, string schemaType, string? description)>();

        foreach (var prop in comp.Properties)
        {
            var jsonName = prop.AssetJsonName ?? prop.JsonName;
            if (string.IsNullOrEmpty(jsonName))
            {
                continue;
            }

            var (schemaType, description) = GetJsonSchemaType(prop.Type, prop.AssetType);
            allProps.Add((jsonName!, schemaType, description));
        }

        sb.AppendLine($"    \"{comp.TypeName}\": {{");
        sb.AppendLine("      \"type\": \"object\",");
        sb.AppendLine("      \"required\": [\"type\"],");
        sb.AppendLine("      \"properties\": {");

        var typeComma = allProps.Count > 0 ? "," : "";
        sb.AppendLine($"        \"type\": {{ \"const\": \"{comp.TypeName}\" }}{typeComma}");

        for (var i = 0; i < allProps.Count; i++)
        {
            var (jsonName, schemaType, description) = allProps[i];
            var propComma = i < allProps.Count - 1 ? "," : "";

            if (description != null)
            {
                sb.AppendLine($"        \"{jsonName}\": {{ {schemaType}, \"description\": \"{description}\" }}{propComma}");
            }
            else
            {
                sb.AppendLine($"        \"{jsonName}\": {{ {schemaType} }}{propComma}");
            }
        }

        sb.AppendLine("      }");
        sb.AppendLine($"    }}{comma}");
    }

    private static (string schemaType, string? description) GetJsonSchemaType(string csharpType, int? assetType)
    {
        if (assetType.HasValue)
        {
            var assetTypeName = assetType.Value switch
            {
                0 => "Mesh",
                1 => "Material",
                2 => "Texture",
                3 => "Shader",
                4 => "Skeleton",
                5 => "Animation",
                _ => "Asset"
            };
            return ("\"type\": \"string\"", $"{assetTypeName} asset reference (pack:asset/selector)");
        }

        var isNullable = csharpType.EndsWith("?");
        var baseType = isNullable ? csharpType.TrimEnd('?') : csharpType;
        baseType = baseType.Replace("global::", "");
        var simpleType = baseType.Contains(".") ? baseType.Substring(baseType.LastIndexOf('.') + 1) : baseType;

        return simpleType switch
        {
            "String" or "string" => ("\"type\": \"string\"", null),
            "Int32" or "int" => ("\"type\": \"integer\"", null),
            "Int64" or "long" => ("\"type\": \"integer\"", null),
            "Single" or "float" => ("\"type\": \"number\"", null),
            "Double" or "double" => ("\"type\": \"number\"", null),
            "Boolean" or "bool" => ("\"type\": \"boolean\"", null),
            "UInt32" or "uint" => ("\"type\": \"integer\", \"minimum\": 0", null),
            "Vector3" => ("\"$ref\": \"#/$defs/vector3\"", null),
            "Vector2" => ("\"type\": \"array\", \"items\": { \"type\": \"number\" }, \"minItems\": 2, \"maxItems\": 2", null),
            "Quaternion" => ("\"type\": \"array\", \"items\": { \"type\": \"number\" }, \"minItems\": 4, \"maxItems\": 4", null),
            "PhysicsShape" => ("\"$ref\": \"#/$defs/physicsShape\"", "Physics collision shape"),
            "PhysicsBodyType" => ("\"enum\": [\"dynamic\", \"static\", \"kinematic\"]", "Physics body type"),
            "PhysicsShapeType" => ("\"enum\": [\"box\", \"sphere\", \"capsule\", \"cylinder\"]", "Shape type"),
            _ when baseType.Contains("Enum") || csharpType.Contains("Enum") => ("\"type\": \"string\"", "Enum value"),
            _ => ("\"type\": \"object\"", $"Complex type: {simpleType}")
        };
    }

    private class ComponentInfo
    {
        public string Namespace { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string TypeName { get; set; } = "";
        public bool GenerateFactory { get; set; }
        public List<PropertyInfo> Properties { get; set; } = [];
        public List<PartialPropertyInfo> PartialProperties { get; set; } = [];
    }

    private class PropertyInfo
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public ITypeSymbol? TypeSymbol { get; set; }
        public string? JsonName { get; set; }
        public int? AssetType { get; set; }
        public string? AssetJsonName { get; set; }
    }

    private class PartialPropertyInfo
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
    }
}
