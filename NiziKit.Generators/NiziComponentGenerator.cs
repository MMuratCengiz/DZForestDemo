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

        // Generate module initializer for component registration
        if (componentInfos.Any(c => c.GenerateFactory))
        {
            var registrationSource = GenerateModuleInitializer(componentInfos);
            context.AddSource("ComponentRegistration.g.cs", SourceText.From(registrationSource, Encoding.UTF8));
        }
    }

    private static ComponentInfo ExtractComponentInfo(INamedTypeSymbol classSymbol, ClassDeclarationSyntax classDecl, Compilation compilation)
    {
        var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
        var className = classSymbol.Name;

        // Get attribute data
        var attribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "NiziComponentAttribute");

        string? typeName = null;
        var generateFactory = true;

        if (attribute != null)
        {
            foreach (var namedArg in attribute.NamedArguments)
            {
                if (namedArg.Key == "TypeName" && namedArg.Value.Value is string tn)
                {
                    typeName = tn;
                }
                else if (namedArg.Key == "GenerateFactory" && namedArg.Value.Value is bool gf)
                {
                    generateFactory = gf;
                }
            }
        }

        // Default type name: remove "Component" suffix and lowercase
        if (string.IsNullOrEmpty(typeName))
        {
            typeName = className;
            if (typeName.EndsWith("Component"))
            {
                typeName = typeName.Substring(0, typeName.Length - 9);
            }
            typeName = typeName.ToLowerInvariant();
        }

        // Extract properties
        var properties = new List<PropertyInfo>();
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
            {
                continue;
            }

            var jsonPropAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "JsonPropertyAttribute");
            var assetRefAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "AssetRefAttribute");

            if (jsonPropAttr != null || assetRefAttr != null)
            {
                var propInfo = new PropertyInfo
                {
                    Name = property.Name,
                    Type = property.Type.ToDisplayString(),
                    TypeSymbol = property.Type
                };

                if (jsonPropAttr != null && jsonPropAttr.ConstructorArguments.Length > 0)
                {
                    propInfo.JsonName = jsonPropAttr.ConstructorArguments[0].Value as string;
                }

                if (assetRefAttr != null)
                {
                    if (assetRefAttr.ConstructorArguments.Length >= 2)
                    {
                        propInfo.AssetType = (int?)assetRefAttr.ConstructorArguments[0].Value;
                        propInfo.AssetJsonName = assetRefAttr.ConstructorArguments[1].Value as string;
                    }
                }

                properties.Add(propInfo);
            }
        }

        // Get partial properties for Owner generation
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

    private static string GenerateModuleInitializer(List<ComponentInfo> components)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using NiziKit.Core;");
        sb.AppendLine("using NiziKit.Components;");
        sb.AppendLine();
        sb.AppendLine("namespace NiziKit.Generated;");
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

        // Generate factory classes
        foreach (var comp in components.Where(c => c.GenerateFactory))
        {
            GenerateFactoryClass(sb, comp);
        }

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
                // Asset reference property
                var resolverMethod = prop.AssetType switch
                {
                    0 => "ResolveMesh",      // Mesh
                    1 => "ResolveMaterial",  // Material
                    2 => "ResolveTexture",   // Texture
                    3 => "ResolveShader",    // Shader
                    _ => null
                };

                if (resolverMethod != null)
                {
                    sb.AppendLine($"            if (properties != null && properties.TryGetValue(\"{prop.AssetJsonName}\", out var {prop.Name.ToLower()}Ref) && {prop.Name.ToLower()}Ref.ValueKind == JsonValueKind.String)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                component.{prop.Name} = resolver.{resolverMethod}({prop.Name.ToLower()}Ref.GetString()!);");
                    sb.AppendLine("            }");
                }
            }
            else if (!string.IsNullOrEmpty(prop.JsonName))
            {
                // Regular JSON property
                var getterMethod = GetJsonGetterMethod(prop.Type);
                if (getterMethod != null)
                {
                    sb.AppendLine($"            if (properties != null && properties.TryGetValue(\"{prop.JsonName}\", out var {prop.Name.ToLower()}Val))");
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

    private static string? GetJsonGetterMethod(string typeName)
    {
        // Handle nullable types
        var isNullable = typeName.EndsWith("?");
        var baseType = isNullable ? typeName.TrimEnd('?') : typeName;

        return baseType switch
        {
            "string" or "System.String" => $"{baseType.ToLower()}Val.GetString()",
            "int" or "System.Int32" => $"{baseType.ToLower()}Val.GetInt32()",
            "float" or "System.Single" => $"{baseType.ToLower()}Val.GetSingle()",
            "double" or "System.Double" => $"{baseType.ToLower()}Val.GetDouble()",
            "bool" or "System.Boolean" => $"{baseType.ToLower()}Val.GetBoolean()",
            "uint" or "System.UInt32" => $"{baseType.ToLower()}Val.GetUInt32()",
            _ => null
        };
    }

    private class ComponentInfo
    {
        public string Namespace { get; set; } = "";
        public string ClassName { get; set; } = "";
        public string TypeName { get; set; } = "";
        public bool GenerateFactory { get; set; }
        public List<PropertyInfo> Properties { get; set; } = new();
        public List<PartialPropertyInfo> PartialProperties { get; set; } = new();
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
