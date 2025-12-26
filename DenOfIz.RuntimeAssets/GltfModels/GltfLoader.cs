using System.Numerics;
using DenOfIz;

namespace RuntimeAssets.GltfModels;

public sealed class GltfLoadResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<MeshData> Meshes { get; init; } = [];
    public IReadOnlyList<MaterialData> Materials { get; init; } = [];
    public IReadOnlyList<Matrix4x4> InverseBindMatrices { get; init; } = [];
    public IReadOnlyList<GltfLoadWarning> Warnings { get; init; } = [];
    public IReadOnlyList<GltfNodeInfo> Nodes { get; init; } = [];
    public IReadOnlyList<GltfAnimationInfo> Animations { get; init; } = [];
    public IReadOnlyList<GltfSkinInfo> Skins { get; init; } = [];

    public static GltfLoadResult Failed(string error)
    {
        return new GltfLoadResult { Success = false, ErrorMessage = error };
    }
}

public readonly record struct GltfLoadWarning(string Message, string? Context = null);

public sealed class GltfNodeInfo
{
    public required string Name { get; init; }
    public required int Index { get; init; }
    public int? MeshIndex { get; init; }
    public int? SkinIndex { get; init; }
    public int? ParentIndex { get; init; }
    public required IReadOnlyList<int> ChildIndices { get; init; }
    public required Matrix4x4 LocalTransform { get; init; }
    public required Matrix4x4 WorldTransform { get; init; }
}

public sealed class GltfAnimationInfo
{
    public required string Name { get; init; }
    public required int Index { get; init; }
    public required float Duration { get; init; }
    public required IReadOnlyList<GltfAnimationChannelInfo> Channels { get; init; }
}

public sealed class GltfAnimationChannelInfo
{
    public required int TargetNode { get; init; }
    public required string Path { get; init; }
    public required string Interpolation { get; init; }
    public required float[] KeyframeTimes { get; init; }
    public required float[] KeyframeValues { get; init; }
}

public sealed class GltfSkinInfo
{
    public required string Name { get; init; }
    public required int Index { get; init; }
    public int? SkeletonRoot { get; init; }
    public required IReadOnlyList<int> JointIndices { get; init; }
    public required IReadOnlyList<Matrix4x4> InverseBindMatrices { get; init; }
    public Matrix4x4 SkeletonRootTransform { get; init; } = Matrix4x4.Identity;
}

public sealed class GltfLoader
{
    private readonly List<GltfLoadWarning> _warnings = [];
    private string _currentPath = "";

    public Action<GltfLogLevel, string>? Logger { get; set; }

    public async Task<GltfLoadResult> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Load(path), cancellationToken);
    }

    public GltfLoadResult Load(string path)
    {
        _warnings.Clear();
        _currentPath = path;

        if (!File.Exists(path))
        {
            return GltfLoadResult.Failed($"File not found: {path}");
        }

        var desc = new GltfDocumentDesc
        {
            LoadExternalBuffers = true,
            LoadExternalImages = true,
            Logger = (level, msg) =>
            {
                AddWarning(msg);
                Logger?.Invoke(level, msg);
            }
        };

        var document = Gltf.Load(path, desc);
        if (document.HasErrors)
        {
            return GltfLoadResult.Failed(string.Join("; ", document.Errors));
        }

        var materials = LoadMaterials(document);
        var meshes = LoadMeshes(document);
        var nodes = LoadNodes(document);
        var animations = LoadAnimations(document);
        var skins = LoadSkins(document);
        var inverseBindMatrices = ExtractAllInverseBindMatrices(skins);

        return new GltfLoadResult
        {
            Success = true,
            Meshes = meshes,
            Materials = materials,
            InverseBindMatrices = inverseBindMatrices,
            Warnings = _warnings.ToList(),
            Nodes = nodes,
            Animations = animations,
            Skins = skins
        };
    }

    private void AddWarning(string message, string? context = null)
    {
        _warnings.Add(new GltfLoadWarning(message, context ?? _currentPath));
    }

    private List<MaterialData> LoadMaterials(GltfDocument document)
    {
        var materials = new List<MaterialData>();
        var basePath = Path.GetDirectoryName(_currentPath) ?? "";

        foreach (var mat in document.Materials)
        {
            var baseColor = Vector4.One;
            var metallic = 0f;
            var roughness = 1f;
            string? baseColorTexture = null;
            string? normalTexture = null;
            string? mrTexture = null;

            var pbr = mat.PbrMetallicRoughness;
            if (pbr != null)
            {
                if (pbr.BaseColorFactor is { Length: >= 4 })
                {
                    baseColor = new Vector4(
                        pbr.BaseColorFactor[0],
                        pbr.BaseColorFactor[1],
                        pbr.BaseColorFactor[2],
                        pbr.BaseColorFactor[3]
                    );
                }

                metallic = pbr.MetallicFactor;
                roughness = pbr.RoughnessFactor;

                baseColorTexture = ResolveTexturePath(document, pbr.BaseColorTexture, basePath);
                mrTexture = ResolveTexturePath(document, pbr.MetallicRoughnessTexture, basePath);
            }

            normalTexture = ResolveTexturePath(document, mat.NormalTexture, basePath);

            materials.Add(new MaterialData
            {
                Name = mat.Name ?? $"Material_{materials.Count}",
                BaseColor = baseColor,
                Metallic = metallic,
                Roughness = roughness,
                BaseColorTexturePath = baseColorTexture,
                NormalTexturePath = normalTexture,
                MetallicRoughnessTexturePath = mrTexture
            });
        }

        return materials;
    }

    private string? ResolveTexturePath(GltfDocument document, GltfTextureInfo? textureInfo, string basePath)
    {
        if (textureInfo == null)
        {
            return null;
        }

        var textureIndex = textureInfo.Index;
        if (textureIndex < 0 || textureIndex >= document.Textures.Count)
        {
            AddWarning($"Invalid texture index: {textureIndex}");
            return null;
        }

        var texture = document.Textures[textureIndex];
        if (!texture.Source.HasValue)
        {
            AddWarning($"Texture {textureIndex} has no source image");
            return null;
        }

        var imageIndex = texture.Source.Value;
        var imagePath = document.GetImageFilePath(imageIndex);

        if (imagePath != null)
        {
            if (File.Exists(imagePath))
            {
                return imagePath;
            }

            AddWarning($"Texture file not found: {imagePath}");
            return null;
        }

        var imageData = document.GetImageData(imageIndex);
        if (!imageData.IsEmpty)
        {
            AddWarning($"Image {imageIndex} is embedded, external path unavailable");
        }

        return null;
    }

    private List<MeshData> LoadMeshes(GltfDocument document)
    {
        var meshes = new List<MeshData>();

        for (var meshIndex = 0; meshIndex < document.Meshes.Count; meshIndex++)
        {
            var mesh = document.Meshes[meshIndex];
            var primitives = new List<MeshPrimitive>();

            for (var primIndex = 0; primIndex < mesh.Primitives.Count; primIndex++)
            {
                var primitive = mesh.Primitives[primIndex];
                if (primitive.Mode != GltfPrimitiveMode.Triangles)
                {
                    AddWarning($"Mesh {meshIndex} primitive {primIndex} uses unsupported mode: {primitive.Mode}");
                    continue;
                }

                var vertices = LoadVertices(document, primitive, meshIndex, primIndex);
                var indices = LoadIndices(document, primitive, meshIndex, primIndex);

                if (vertices.Length == 0)
                {
                    AddWarning($"Mesh {meshIndex} primitive {primIndex} has no vertices");
                    continue;
                }

                primitives.Add(new MeshPrimitive
                {
                    Vertices = vertices,
                    Indices = indices,
                    MaterialIndex = primitive.Material ?? -1
                });
            }

            meshes.Add(new MeshData
            {
                Name = mesh.Name ?? $"Mesh_{meshIndex}",
                Primitives = primitives
            });
        }

        return meshes;
    }

    private Vertex[] LoadVertices(GltfDocument document, GltfPrimitive primitive, int meshIndex, int primIndex)
    {
        if (!primitive.Attributes.TryGetValue("POSITION", out var positionAccessor))
        {
            AddWarning($"Mesh {meshIndex} primitive {primIndex} has no POSITION attribute");
            return [];
        }

        var positions = document.ReadAccessorVec3(positionAccessor);
        GltfCoordinateConversion.ConvertPositionsInPlace(positions); // Negate Z for left-handed

        if (positions.Length == 0)
        {
            return [];
        }

        Vector3[]? normals = null;
        Vector2[]? texCoords = null;
        Vector4[]? tangents = null;
        Vector4[]? joints = null;
        Vector4[]? weights = null;

        if (primitive.Attributes.TryGetValue("NORMAL", out var normalAccessor))
        {
            normals = document.ReadAccessorVec3(normalAccessor);
            GltfCoordinateConversion.ConvertNormalsInPlace(normals); // Negate Z for left-handed
        }

        if (primitive.Attributes.TryGetValue("TEXCOORD_0", out var texCoordAccessor))
        {
            texCoords = document.ReadAccessorVec2(texCoordAccessor);
        }

        if (primitive.Attributes.TryGetValue("TANGENT", out var tangentAccessor))
        {
            tangents = document.ReadAccessorVec4(tangentAccessor);
            GltfCoordinateConversion.ConvertTangentsInPlace(tangents); // Negate Z and W for left-handed
        }

        if (primitive.Attributes.TryGetValue("JOINTS_0", out var jointsAccessor))
        {
            joints = ReadJointsAsVector4(document, jointsAccessor);
        }

        if (primitive.Attributes.TryGetValue("WEIGHTS_0", out var weightsAccessor))
        {
            weights = document.ReadAccessorVec4(weightsAccessor);
        }

        var vertices = new Vertex[positions.Length];

        for (var i = 0; i < positions.Length; i++)
        {
            vertices[i] = new Vertex
            {
                Position = positions[i],
                Normal = (normals != null && i < normals.Length) ? normals[i] : Vector3.UnitY,
                TexCoord = (texCoords != null && i < texCoords.Length) ? texCoords[i] : Vector2.Zero,
                Tangent = (tangents != null && i < tangents.Length) ? tangents[i] : new Vector4(1, 0, 0, 1),
                BoneWeights = (weights != null && i < weights.Length) ? weights[i] : Vector4.Zero,
                BoneIndices = (joints != null && i < joints.Length)
                    ? new UInt4
                    {
                        X = (uint)joints[i].X,
                        Y = (uint)joints[i].Y,
                        Z = (uint)joints[i].Z,
                        W = (uint)joints[i].W
                    }
                    : default
            };
        }

        return vertices;
    }

    private Vector4[] ReadJointsAsVector4(GltfDocument document, int accessorIndex)
    {
        if (accessorIndex < 0 || accessorIndex >= document.Accessors.Count)
        {
            return [];
        }

        var accessor = document.Accessors[accessorIndex];

        // JOINTS_0 can be stored as unsigned bytes or unsigned shorts
        return accessor.ComponentType switch
        {
            GltfComponentType.UnsignedByte => document.ReadAccessor<JointsByte>(accessorIndex)
                .Select(j => new Vector4(j.X, j.Y, j.Z, j.W))
                .ToArray(),
            GltfComponentType.UnsignedShort => document.ReadAccessor<JointsShort>(accessorIndex)
                .Select(j => new Vector4(j.X, j.Y, j.Z, j.W))
                .ToArray(),
            _ => document.ReadAccessorVec4(accessorIndex)
        };
    }

    private struct JointsByte
    {
        public byte X, Y, Z, W;
    }

    private struct JointsShort
    {
        public ushort X, Y, Z, W;
    }

    private uint[] LoadIndices(GltfDocument document, GltfPrimitive primitive, int meshIndex, int primIndex)
    {
        uint[] indices;

        if (primitive.Indices.HasValue)
        {
            indices = document.ReadIndices(primitive.Indices.Value);
        }
        else
        {
            if (!primitive.Attributes.TryGetValue("POSITION", out var posAccessor) ||
                posAccessor < 0 || posAccessor >= document.Accessors.Count)
            {
                return [];
            }

            var count = document.Accessors[posAccessor].Count;
            indices = new uint[count];
            for (var i = 0; i < count; i++)
            {
                indices[i] = (uint)i;
            }
        }

        GltfCoordinateConversion.ReverseWindingOrder(indices);
        return indices;
    }

    private List<GltfNodeInfo> LoadNodes(GltfDocument document)
    {
        var nodes = new List<GltfNodeInfo>();
        var parentMap = BuildParentMap(document);

        for (var i = 0; i < document.Nodes.Count; i++)
        {
            var node = document.Nodes[i];
            var localTransform = document.GetNodeLocalTransform(i);
            var worldTransform = document.GetNodeWorldTransform(i);

            var convertedLocal = GltfCoordinateConversion.ConvertMatrixHandedness(localTransform);
            var convertedWorld = GltfCoordinateConversion.ConvertMatrixHandedness(worldTransform);

            nodes.Add(new GltfNodeInfo
            {
                Name = node.Name ?? $"Node_{i}",
                Index = i,
                MeshIndex = node.Mesh,
                SkinIndex = node.Skin,
                ParentIndex = parentMap.GetValueOrDefault(i, -1) >= 0 ? parentMap[i] : null,
                ChildIndices = node.Children ?? [],
                LocalTransform = convertedLocal,
                WorldTransform = convertedWorld
            });
        }

        return nodes;
    }

    private Dictionary<int, int> BuildParentMap(GltfDocument document)
    {
        var parentMap = new Dictionary<int, int>();

        for (var i = 0; i < document.Nodes.Count; i++)
        {
            var node = document.Nodes[i];
            if (node.Children != null)
            {
                foreach (var childIndex in node.Children)
                {
                    parentMap[childIndex] = i;
                }
            }
        }

        return parentMap;
    }

    private List<GltfAnimationInfo> LoadAnimations(GltfDocument document)
    {
        var animations = new List<GltfAnimationInfo>();

        for (var animIndex = 0; animIndex < document.Animations.Count; animIndex++)
        {
            var anim = document.Animations[animIndex];
            var channels = new List<GltfAnimationChannelInfo>();
            var maxDuration = 0f;

            foreach (var channel in anim.Channels)
            {
                if (!channel.Target.Node.HasValue)
                {
                    AddWarning($"Animation {animIndex} has channel without target node");
                    continue;
                }

                if (channel.Sampler < 0 || channel.Sampler >= anim.Samplers.Count)
                {
                    AddWarning($"Animation {animIndex} has invalid sampler index: {channel.Sampler}");
                    continue;
                }

                var sampler = anim.Samplers[channel.Sampler];
                var times = document.ReadAccessorFloat(sampler.Input);

                float[] values;
                var path = channel.Target.Path.ToLowerInvariant();
                switch (path)
                {
                    case "translation":
                    {
                        var translations = document.ReadAccessorVec3(sampler.Output);
                        GltfCoordinateConversion.ConvertPositionsInPlace(translations);
                        values = new float[translations.Length * 3];
                        for (var i = 0; i < translations.Length; i++)
                        {
                            values[i * 3] = translations[i].X;
                            values[i * 3 + 1] = translations[i].Y;
                            values[i * 3 + 2] = translations[i].Z;
                        }

                        break;
                    }
                    case "rotation":
                    {
                        var rotations = document.ReadAccessorVec4(sampler.Output);
                        GltfCoordinateConversion.ConvertQuaternionsInPlace(rotations);
                        values = new float[rotations.Length * 4];
                        for (var i = 0; i < rotations.Length; i++)
                        {
                            values[i * 4] = rotations[i].X;
                            values[i * 4 + 1] = rotations[i].Y;
                            values[i * 4 + 2] = rotations[i].Z;
                            values[i * 4 + 3] = rotations[i].W;
                        }

                        break;
                    }
                    default:
                        values = document.ReadAccessorFloat(sampler.Output);
                        break;
                }

                if (times.Length > 0)
                {
                    maxDuration = Math.Max(maxDuration, times[^1]);
                }

                channels.Add(new GltfAnimationChannelInfo
                {
                    TargetNode = channel.Target.Node.Value,
                    Path = channel.Target.Path,
                    Interpolation = sampler.Interpolation,
                    KeyframeTimes = times,
                    KeyframeValues = values
                });
            }

            animations.Add(new GltfAnimationInfo
            {
                Name = anim.Name ?? $"Animation_{animIndex}",
                Index = animIndex,
                Duration = maxDuration,
                Channels = channels
            });
        }

        return animations;
    }

    private List<GltfSkinInfo> LoadSkins(GltfDocument document)
    {
        var skins = new List<GltfSkinInfo>();

        for (var skinIndex = 0; skinIndex < document.Skins.Count; skinIndex++)
        {
            var skin = document.Skins[skinIndex];

            var inverseBindMatrices = skin.InverseBindMatrices.HasValue
                ? document.ReadAccessorMat4(skin.InverseBindMatrices.Value)
                    .Select(GltfCoordinateConversion.ConvertMatrixHandedness).ToList()
                : Enumerable.Repeat(Matrix4x4.Identity, skin.Joints.Count).ToList();

            while (inverseBindMatrices.Count < skin.Joints.Count)
            {
                inverseBindMatrices.Add(Matrix4x4.Identity);
            }

            var skeletonRootTransform = Matrix4x4.Identity;
            if (skin.Skeleton.HasValue)
            {
                var rootWorldTransform = document.GetNodeWorldTransform(skin.Skeleton.Value);
                skeletonRootTransform = GltfCoordinateConversion.ConvertMatrixHandedness(rootWorldTransform);
            }

            skins.Add(new GltfSkinInfo
            {
                Name = skin.Name ?? $"Skin_{skinIndex}",
                Index = skinIndex,
                SkeletonRoot = skin.Skeleton,
                JointIndices = skin.Joints,
                InverseBindMatrices = inverseBindMatrices,
                SkeletonRootTransform = skeletonRootTransform
            });
        }

        return skins;
    }

    private static List<Matrix4x4> ExtractAllInverseBindMatrices(List<GltfSkinInfo> skins)
    {
        var allMatrices = new List<Matrix4x4>();
        foreach (var skin in skins)
        {
            allMatrices.AddRange(skin.InverseBindMatrices);
        }

        return allMatrices;
    }
}