using NiziKit.Assets;
using NiziKit.ContentPIpeline;

namespace NiziKit.GLTF;

public sealed class GltfLoadOptions
{
    public bool ConvertToLeftHanded { get; set; } = true;
}

public sealed class GltfModel
{
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public GltfLoadOptions Options { get; private set; } = new();

    public GltfDocument Document { get; private set; } = null!;
    public GltfSceneData Scene { get; private set; } = new();

    public List<Mesh> Meshes { get; private set; } = [];
    public List<GltfMaterialData> Materials { get; private set; } = [];
    public List<GltfImageData> Images { get; private set; } = [];
    public List<Skeleton> Skeletons { get; private set; } = [];
    public List<Animation> Animations { get; private set; } = [];

    public static GltfModel Load(string path, GltfLoadOptions? options = null)
    {
        var bytes = Content.ReadBytes($"Models/{path}");
        return LoadFromBytes(bytes, path, options);
    }

    public static GltfModel Load(byte[] bytes, string name, GltfLoadOptions? options = null)
    {
        return LoadFromBytes(bytes, name, options);
    }

    public static async Task<GltfModel> LoadAsync(string path, GltfLoadOptions? options = null, CancellationToken ct = default)
    {
        var bytes = await Content.ReadBytesAsync($"Models/{path}", ct);
        return LoadFromBytes(bytes, path, options);
    }

    public static GltfModel LoadFromStream(Stream stream, string name, GltfLoadOptions? options = null)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return LoadFromBytes(ms.ToArray(), name, options);
    }

    public static async Task<GltfModel> LoadFromStreamAsync(Stream stream, string name, GltfLoadOptions? options = null, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return LoadFromBytes(ms.ToArray(), name, options);
    }

    public static GltfModel LoadFromBytes(byte[] bytes, string name, GltfLoadOptions? options = null)
    {
        options ??= new GltfLoadOptions();
        var basePath = Path.GetDirectoryName(name)?.Replace('\\', '/');

        Func<string, byte[]>? loadBuffer = null;
        if (!string.IsNullOrEmpty(basePath))
        {
            loadBuffer = uri => Content.ReadBytes($"Models/{basePath}/{uri}");
        }

        var document = GltfReader.Read(bytes, loadBuffer, basePath);

        var model = new GltfModel
        {
            Name = Path.GetFileNameWithoutExtension(name),
            SourcePath = name,
            Document = document,
            Options = options
        };

        model.ExtractAll();
        return model;
    }

    private void ExtractAll()
    {
        Scene = GltfSceneExtractor.ExtractDefaultScene(Document, Options.ConvertToLeftHanded);

        var skinnedMeshIndices = GltfMeshExtractor.GetSkinnedMeshIndices(Document);
        Meshes = GltfMeshExtractor.ExtractMeshes(Document, skinnedMeshIndices, Options.ConvertToLeftHanded);

        Materials = GltfMaterialExtractor.ExtractMaterials(Document);
        Images = GltfMaterialExtractor.ExtractImages(Document);

        var nodeToJointMap = BuildNodeToJointMap();
        var rawSkeletons = GltfSkeletonExtractor.ExtractSkeletons(Document);
        var rawAnimations = GltfAnimationExtractor.ExtractAnimations(Document);

        Skeletons = rawSkeletons
            .Select(s => GltfSkeletonExtractor.ToSkeleton(s, Options.ConvertToLeftHanded))
            .ToList();

        Animations = rawAnimations
            .Select(a => GltfAnimationExtractor.ToAnimation(a, nodeToJointMap, Options.ConvertToLeftHanded))
            .ToList();

        AssignMaterialIndices();
    }

    private void AssignMaterialIndices()
    {
        if (Document.Root.Meshes == null)
        {
            return;
        }

        for (var i = 0; i < Document.Root.Meshes.Count && i < Meshes.Count; i++)
        {
            var gltfMesh = Document.Root.Meshes[i];
            if (gltfMesh.Primitives.Count > 0)
            {
                var materialIndex = gltfMesh.Primitives[0].Material;
                if (materialIndex.HasValue)
                {
                    Meshes[i].MaterialIndex = materialIndex.Value;
                }
            }
        }
    }

    public Dictionary<int, int> BuildNodeToJointMap(int skinIndex = 0)
    {
        var result = new Dictionary<int, int>();

        if (Document.Root.Skins == null || skinIndex >= Document.Root.Skins.Count)
        {
            return result;
        }

        var skin = Document.Root.Skins[skinIndex];
        for (var i = 0; i < skin.Joints.Count; i++)
        {
            result[skin.Joints[i]] = i;
        }

        return result;
    }

    public byte[]? GetImageData(int imageIndex)
    {
        if (imageIndex < 0 || imageIndex >= Images.Count)
        {
            return null;
        }

        var image = Images[imageIndex];

        if (image.Data != null)
        {
            return image.Data;
        }

        if (!string.IsNullOrEmpty(image.Uri) && !image.Uri.StartsWith("data:"))
        {
            var basePath = Path.GetDirectoryName(SourcePath)?.Replace('\\', '/');
            var imagePath = string.IsNullOrEmpty(basePath) ? image.Uri : $"{basePath}/{image.Uri}";
            return Content.ReadBytes($"Models/{imagePath}");
        }

        return null;
    }

    public int? GetTextureImageIndex(int textureIndex)
    {
        return GltfMaterialExtractor.GetImageIndex(Document, textureIndex);
    }

    public Model ToModel()
    {
        return new Model
        {
            Name = Name,
            SourcePath = SourcePath,
            Meshes = Meshes.ToList()
        };
    }
}
