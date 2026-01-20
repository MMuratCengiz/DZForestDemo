namespace NiziKit.Assets;

public sealed class MeshAttributeData
{
    public required string Name { get; init; }
    public required byte[] Data { get; init; }
    public required VertexAttributeType Type { get; init; }
}

public sealed class MeshAttributeSet
{
    public required Dictionary<string, MeshAttributeData> Attributes { get; init; }
    public required int VertexCount { get; init; }
    public required uint[] Indices { get; init; }

    public bool HasAttribute(string gltfName)
    {
        return Attributes.ContainsKey(gltfName);
    }

    public MeshAttributeData? GetAttribute(string gltfName)
    {
        return Attributes.GetValueOrDefault(gltfName);
    }

    public static string MapSemanticToGltf(string semantic, int semanticIndex)
    {
        return semantic switch
        {
            "POSITION" => "POSITION",
            "NORMAL" => "NORMAL",
            "TANGENT" => "TANGENT",
            "TEXCOORD" => $"TEXCOORD_{semanticIndex}",
            "COLOR" => $"COLOR_{semanticIndex}",
            "BLENDWEIGHT" => "WEIGHTS_0",
            "BLENDINDICES" => "JOINTS_0",
            _ => semantic
        };
    }

    public static (string Semantic, int SemanticIndex) MapGltfToSemantic(string gltfName)
    {
        if (gltfName.StartsWith("TEXCOORD_"))
        {
            var index = int.Parse(gltfName.AsSpan(9));
            return ("TEXCOORD", index);
        }

        if (gltfName.StartsWith("COLOR_"))
        {
            var index = int.Parse(gltfName.AsSpan(6));
            return ("COLOR", index);
        }

        return gltfName switch
        {
            "POSITION" => ("POSITION", 0),
            "NORMAL" => ("NORMAL", 0),
            "TANGENT" => ("TANGENT", 0),
            "WEIGHTS_0" => ("BLENDWEIGHT", 0),
            "JOINTS_0" => ("BLENDINDICES", 0),
            _ => (gltfName, 0)
        };
    }
}
