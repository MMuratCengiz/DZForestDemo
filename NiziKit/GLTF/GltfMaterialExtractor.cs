using System.Numerics;
using NiziKit.GLTF.Data;

namespace NiziKit.GLTF;

public sealed class GltfMaterialData
{
    public string Name { get; set; } = string.Empty;
    public Vector4 BaseColorFactor { get; set; } = Vector4.One;
    public float MetallicFactor { get; set; } = 1.0f;
    public float RoughnessFactor { get; set; } = 1.0f;
    public Vector3 EmissiveFactor { get; set; } = Vector3.Zero;
    public string AlphaMode { get; set; } = GltfAlphaMode.Opaque;
    public float AlphaCutoff { get; set; } = 0.5f;
    public bool DoubleSided { get; set; }

    public GltfTextureReference? BaseColorTexture { get; set; }
    public GltfTextureReference? MetallicRoughnessTexture { get; set; }
    public GltfTextureReference? NormalTexture { get; set; }
    public GltfTextureReference? OcclusionTexture { get; set; }
    public GltfTextureReference? EmissiveTexture { get; set; }
}

public sealed class GltfTextureReference
{
    public int TextureIndex { get; set; }
    public int TexCoord { get; set; }
    public float Scale { get; set; } = 1.0f;
    public float Strength { get; set; } = 1.0f;
}

public sealed class GltfImageData
{
    public string? Name { get; set; }
    public string? Uri { get; set; }
    public string? MimeType { get; set; }
    public byte[]? Data { get; set; }
}

public static class GltfMaterialExtractor
{
    public static List<GltfMaterialData> ExtractMaterials(GltfDocument document)
    {
        var result = new List<GltfMaterialData>();
        var root = document.Root;

        if (root.Materials == null)
        {
            return result;
        }

        for (var i = 0; i < root.Materials.Count; i++)
        {
            var material = root.Materials[i];
            result.Add(ExtractMaterial(material, i));
        }

        return result;
    }

    public static List<GltfImageData> ExtractImages(GltfDocument document)
    {
        var result = new List<GltfImageData>();
        var root = document.Root;

        if (root.Images == null)
        {
            return result;
        }

        foreach (var image in root.Images)
        {
            var imageData = new GltfImageData
            {
                Name = image.Name,
                Uri = image.Uri,
                MimeType = image.MimeType
            };

            if (image.BufferView.HasValue)
            {
                var data = document.GetBufferData(image.BufferView.Value);
                imageData.Data = data.ToArray();
            }
            else if (image.Uri != null && image.Uri.StartsWith("data:"))
            {
                imageData.Data = DecodeDataUri(image.Uri);
            }

            result.Add(imageData);
        }

        return result;
    }

    public static int? GetImageIndex(GltfDocument document, int textureIndex)
    {
        if (document.Root.Textures == null || textureIndex >= document.Root.Textures.Count)
        {
            return null;
        }

        return document.Root.Textures[textureIndex].Source;
    }

    private static GltfMaterialData ExtractMaterial(GltfMaterial material, int index)
    {
        var data = new GltfMaterialData
        {
            Name = material.Name ?? $"Material_{index}",
            AlphaMode = material.AlphaMode,
            AlphaCutoff = material.AlphaCutoff,
            DoubleSided = material.DoubleSided
        };

        if (material.EmissiveFactor is { Length: >= 3 })
        {
            data.EmissiveFactor = new Vector3(
                material.EmissiveFactor[0],
                material.EmissiveFactor[1],
                material.EmissiveFactor[2]);
        }

        if (material.PbrMetallicRoughness != null)
        {
            var pbr = material.PbrMetallicRoughness;

            if (pbr.BaseColorFactor is { Length: >= 4 })
            {
                data.BaseColorFactor = new Vector4(
                    pbr.BaseColorFactor[0],
                    pbr.BaseColorFactor[1],
                    pbr.BaseColorFactor[2],
                    pbr.BaseColorFactor[3]);
            }

            data.MetallicFactor = pbr.MetallicFactor;
            data.RoughnessFactor = pbr.RoughnessFactor;

            if (pbr.BaseColorTexture != null)
            {
                data.BaseColorTexture = new GltfTextureReference
                {
                    TextureIndex = pbr.BaseColorTexture.Index,
                    TexCoord = pbr.BaseColorTexture.TexCoord
                };
            }

            if (pbr.MetallicRoughnessTexture != null)
            {
                data.MetallicRoughnessTexture = new GltfTextureReference
                {
                    TextureIndex = pbr.MetallicRoughnessTexture.Index,
                    TexCoord = pbr.MetallicRoughnessTexture.TexCoord
                };
            }
        }

        if (material.NormalTexture != null)
        {
            data.NormalTexture = new GltfTextureReference
            {
                TextureIndex = material.NormalTexture.Index,
                TexCoord = material.NormalTexture.TexCoord,
                Scale = material.NormalTexture.Scale
            };
        }

        if (material.OcclusionTexture != null)
        {
            data.OcclusionTexture = new GltfTextureReference
            {
                TextureIndex = material.OcclusionTexture.Index,
                TexCoord = material.OcclusionTexture.TexCoord,
                Strength = material.OcclusionTexture.Strength
            };
        }

        if (material.EmissiveTexture != null)
        {
            data.EmissiveTexture = new GltfTextureReference
            {
                TextureIndex = material.EmissiveTexture.Index,
                TexCoord = material.EmissiveTexture.TexCoord
            };
        }

        return data;
    }

    private static byte[] DecodeDataUri(string uri)
    {
        var commaIndex = uri.IndexOf(',');
        if (commaIndex < 0)
        {
            return [];
        }

        var data = uri.AsSpan(commaIndex + 1);
        return Convert.FromBase64String(data.ToString());
    }
}
