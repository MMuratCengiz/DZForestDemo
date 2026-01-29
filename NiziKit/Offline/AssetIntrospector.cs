using Silk.NET.Assimp;
using File = System.IO.File;

namespace NiziKit.Offline;

public class MeshInfo
{
    public string Name { get; set; } = string.Empty;
    public uint VertexCount { get; set; }
    public uint IndexCount { get; set; }
    public bool HasSkin { get; set; }
}

public class SkeletonInfo
{
    public int JointCount { get; set; }
    public string RootJointName { get; set; } = string.Empty;
}

public class AnimationInfo
{
    public string Name { get; set; } = string.Empty;
    public double Duration { get; set; }
    public double TicksPerSecond { get; set; }
    public uint ChannelCount { get; set; }
}

public class AssetIntrospectionResult
{
    public string SourcePath { get; set; } = string.Empty;
    public List<MeshInfo> Meshes { get; set; } = [];
    public SkeletonInfo? Skeleton { get; set; }
    public List<AnimationInfo> Animations { get; set; } = [];
    public string? Error { get; set; }

    public bool HasError => !string.IsNullOrEmpty(Error);
}

public sealed class AssetIntrospector : IDisposable
{
    private readonly Assimp _assimp = Assimp.GetApi();

    public unsafe AssetIntrospectionResult Introspect(string filePath)
    {
        var result = new AssetIntrospectionResult { SourcePath = filePath };

        if (!File.Exists(filePath))
        {
            result.Error = $"File not found: {filePath}";
            return result;
        }

        var importFlags = (uint)(PostProcessSteps.Triangulate | PostProcessSteps.ValidateDataStructure);
        var scene = _assimp.ImportFile(filePath, importFlags);

        if (scene == null || scene->MRootNode == null)
        {
            var errorPtr = _assimp.GetErrorString();
            var error = errorPtr != null ? new string((sbyte*)errorPtr) : "Unknown error";
            result.Error = $"Failed to load: {error}";
            return result;
        }

        try
        {
            // Meshes
            for (var i = 0u; i < scene->MNumMeshes; i++)
            {
                var mesh = scene->MMeshes[i];
                var name = mesh->MName.AsString ?? $"Mesh_{i}";
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"Mesh_{i}";
                }

                result.Meshes.Add(new MeshInfo
                {
                    Name = name,
                    VertexCount = mesh->MNumVertices,
                    IndexCount = CountIndices(mesh),
                    HasSkin = mesh->MNumBones > 0
                });
            }

            // Skeleton - collect unique bones across all meshes
            var boneNames = new HashSet<string>();
            string? rootBoneName = null;
            for (var i = 0u; i < scene->MNumMeshes; i++)
            {
                var mesh = scene->MMeshes[i];
                for (var b = 0u; b < mesh->MNumBones; b++)
                {
                    var bone = mesh->MBones[b];
                    var boneName = bone->MName.AsString ?? "";
                    boneNames.Add(boneName);
                    rootBoneName ??= boneName;
                }
            }

            if (boneNames.Count > 0)
            {
                result.Skeleton = new SkeletonInfo
                {
                    JointCount = boneNames.Count,
                    RootJointName = rootBoneName ?? ""
                };
            }

            // Animations
            for (var i = 0u; i < scene->MNumAnimations; i++)
            {
                var anim = scene->MAnimations[i];
                var name = anim->MName.AsString ?? $"Animation_{i}";
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"Animation_{i}";
                }

                var tps = anim->MTicksPerSecond > 0 ? anim->MTicksPerSecond : 24.0;
                result.Animations.Add(new AnimationInfo
                {
                    Name = name,
                    Duration = anim->MDuration / tps,
                    TicksPerSecond = tps,
                    ChannelCount = anim->MNumChannels
                });
            }

            return result;
        }
        finally
        {
            _assimp.ReleaseImport(scene);
        }
    }

    private static unsafe uint CountIndices(Mesh* mesh)
    {
        uint count = 0;
        for (var f = 0u; f < mesh->MNumFaces; f++)
        {
            count += mesh->MFaces[f].MNumIndices;
        }
        return count;
    }

    public void Dispose()
    {
        _assimp.Dispose();
    }
}
