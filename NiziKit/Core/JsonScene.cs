using System.Numerics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NiziKit.Assets;
using NiziKit.Assets.Pack;
using NiziKit.Assets.Serde;
using NiziKit.Components;
using NiziKit.ContentPipeline;
using NiziKit.Light;
using NiziKit.Graphics.Renderer.Forward;
using NiziKit.Physics;

namespace NiziKit.Core;

public class JsonScene(string jsonPath) : Scene(Path.GetFileNameWithoutExtension(jsonPath)), IAssetResolver
{
    private static readonly ILogger Logger = Log.Get<JsonScene>();

    public override void Load()
    {
        SourcePath = jsonPath;
        var json = Content.ReadText(jsonPath);
        var sceneData = SceneJson.FromJson(json);

        Name = sceneData.Name;

        var assetRefs = CollectAssetReferences(sceneData.Objects, sceneData.Skybox);
        LoadRequiredAssets(assetRefs);
        PrewarmMeshes(sceneData.Objects);
        LoadCamera(sceneData.Camera);
        LoadCameras(sceneData.Cameras);
        LoadLights(sceneData.Lights);
        LoadObjects(sceneData.Objects);
        LoadSkybox(sceneData.Skybox);
    }

    public override async Task LoadAsync(CancellationToken ct = default)
    {
        SourcePath = jsonPath;
        var json = await Content.ReadTextAsync(jsonPath, ct);
        var sceneData = SceneJson.FromJson(json);

        Name = sceneData.Name;

        var assetRefs = CollectAssetReferences(sceneData.Objects, sceneData.Skybox);
        await LoadRequiredAssetsAsync(assetRefs, ct);
        PrewarmMeshes(sceneData.Objects);
        LoadCamera(sceneData.Camera);
        LoadCameras(sceneData.Cameras);
        LoadLights(sceneData.Lights);
        LoadObjects(sceneData.Objects);
        LoadSkybox(sceneData.Skybox);
    }

    private HashSet<string> CollectAssetReferences(List<GameObjectJson>? objects, SkyboxJson? skybox)
    {
        var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectRefsRecursive(objects, refs);
        return refs;
    }

    private void CollectRefsRecursive(List<GameObjectJson>? objects, HashSet<string> refs)
    {
        if (objects == null)
        {
            return;
        }

        foreach (var obj in objects)
        {
            if (obj.Components != null)
            {
                foreach (var comp in obj.Components)
                {
                    if (comp.Properties != null)
                    {
                        CollectAssetRef(comp.Properties, "mesh", refs);
                        CollectAssetRef(comp.Properties, "albedo", refs);
                        CollectAssetRef(comp.Properties, "normal", refs);
                        CollectAssetRef(comp.Properties, "metallic", refs);
                        CollectAssetRef(comp.Properties, "roughness", refs);
                        CollectAssetRef(comp.Properties, "skeleton", refs);
                        CollectAnimationRefs(comp.Properties, refs);
                    }
                }
            }
            CollectRefsRecursive(obj.Children, refs);
        }
    }

    private void CollectAssetRef(IReadOnlyDictionary<string, JsonElement> props, string key, HashSet<string> refs)
    {
        var value = props.GetStringOrDefault(key);
        if (!string.IsNullOrEmpty(value) && !value.StartsWith("geometry:"))
        {
            refs.Add(ExtractFilePath(value));
        }
    }

    private void CollectAnimationRefs(IReadOnlyDictionary<string, JsonElement> props, HashSet<string> refs)
    {
        if (props.TryGetValue("animations", out var animElement) && animElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var anim in animElement.EnumerateArray())
            {
                if (anim.TryGetProperty("source", out var source) && source.ValueKind == JsonValueKind.String)
                {
                    var sourceStr = source.GetString();
                    if (!string.IsNullOrEmpty(sourceStr))
                    {
                        refs.Add(ExtractFilePath(sourceStr));
                    }
                }
            }
        }
    }

    private string ExtractFilePath(string reference)
    {
        return reference;
    }

    private void LoadRequiredAssets(HashSet<string> assetRefs)
    {
        var packsToLoad = assetRefs
            .Select(r => AssetPacks.GetPackForPath(r))
            .Where(p => p != null && !AssetPacks.IsLoaded(p))
            .Distinct()
            .ToList();

        Parallel.ForEach(packsToLoad, packName => AssetPacks.EnsurePackLoaded(packName!));
    }

    private async Task LoadRequiredAssetsAsync(HashSet<string> assetRefs, CancellationToken ct)
    {
        var packsToLoad = assetRefs
            .Select(AssetPacks.GetPackForPath)
            .Where(p => p != null && !AssetPacks.IsLoaded(p))
            .Distinct()
            .ToList();

        var tasks = packsToLoad.Select(p => AssetPacks.EnsurePackLoadedAsync(p!, ct));
        await Task.WhenAll(tasks);
    }

    private void PrewarmMeshes(List<GameObjectJson>? objects)
    {
        if (objects == null)
        {
            return;
        }

        var meshMaterialPairs = new List<(Mesh mesh, VertexFormat format)>();
        CollectMeshMaterialPairs(objects, meshMaterialPairs);

        Parallel.ForEach(meshMaterialPairs, pair =>
        {
            pair.mesh.GetVertexBuffer(pair.format);
        });
    }

    private void CollectMeshMaterialPairs(List<GameObjectJson> objects, List<(Mesh mesh, VertexFormat format)> pairs)
    {
        foreach (var obj in objects)
        {
            if (obj.Components != null)
            {
                string? meshRef = null;
                string? variant = null;

                foreach (var comp in obj.Components)
                {
                    var shortType = GetShortTypeName(comp.Type);
                    if (shortType.Equals("mesh", StringComparison.OrdinalIgnoreCase))
                    {
                        meshRef = comp.Properties?.GetStringOrDefault("mesh");
                    }
                    else if (shortType.Equals("material", StringComparison.OrdinalIgnoreCase) && comp.Properties != null)
                    {
                        // Read variant from material component tags
                        if (comp.Properties.TryGetValue("tags", out var tagsElement) && tagsElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var tag in tagsElement.EnumerateObject())
                            {
                                if (tag.Name.Equals("variant", StringComparison.OrdinalIgnoreCase) && tag.Value.ValueKind == JsonValueKind.String)
                                {
                                    variant = tag.Value.GetString();
                                }
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(meshRef))
                {
                    var mesh = ResolveMeshFromProperties(
                        new Dictionary<string, JsonElement>
                        {
                            ["mesh"] = JsonSerializer.SerializeToElement(meshRef)
                        });

                    if (mesh != null)
                    {
                        // Use skinned vertex format if variant is SKINNED
                        var isSkinned = !string.IsNullOrEmpty(variant) &&
                                       variant.Equals("SKINNED", StringComparison.OrdinalIgnoreCase);
                        var format = isSkinned ? VertexFormat.Skinned : VertexFormat.Static;
                        pairs.Add((mesh, format));
                    }
                }
            }

            if (obj.Children != null)
            {
                CollectMeshMaterialPairs(obj.Children, pairs);
            }
        }
    }

    private void LoadCamera(CameraJson? cameraData)
    {
        if (cameraData == null)
        {
            return;
        }

        var cameraObj = CreateObject(cameraData.Name ?? "Main Camera");

        if (cameraData.Position != null)
        {
            cameraObj.LocalPosition = ParseVector3(cameraData.Position);
        }

        if (cameraData.Rotation != null)
        {
            cameraObj.LocalRotation = ParseRotation(cameraData.Rotation);
        }

        var cameraComponent = cameraObj.AddComponent<CameraComponent>();

        if (cameraData.FieldOfView.HasValue)
        {
            cameraComponent.FieldOfView = cameraData.FieldOfView.Value;
        }

        if (cameraData.NearPlane.HasValue)
        {
            cameraComponent.NearPlane = cameraData.NearPlane.Value;
        }

        if (cameraData.FarPlane.HasValue)
        {
            cameraComponent.FarPlane = cameraData.FarPlane.Value;
        }

        if (!string.IsNullOrEmpty(cameraData.ProjectionType))
        {
            cameraComponent.ProjectionType = cameraData.ProjectionType.ToLowerInvariant() switch
            {
                "orthographic" => ProjectionType.Orthographic,
                _ => ProjectionType.Perspective
            };
        }

        if (cameraData.OrthographicSize.HasValue)
        {
            cameraComponent.OrthographicSize = cameraData.OrthographicSize.Value;
        }

        if (cameraData.Priority.HasValue)
        {
            cameraComponent.Priority = cameraData.Priority.Value;
        }

        if (cameraData.IsActive.HasValue)
        {
            cameraComponent.IsActiveCamera = cameraData.IsActive.Value;
        }

        if (cameraData.Controller != null)
        {
            var controllerType = cameraData.Controller.Type?.ToLowerInvariant() ?? "freefly";

            if (controllerType == "orbit")
            {
                var orbit = cameraObj.AddComponent<OrbitController>();

                if (cameraData.Controller.LookSensitivity.HasValue)
                {
                    orbit.LookSensitivity = cameraData.Controller.LookSensitivity.Value;
                }

                if (cameraData.Controller.OrbitTarget != null)
                {
                    orbit.OrbitTarget = ParseVector3(cameraData.Controller.OrbitTarget);
                }

                if (cameraData.Controller.OrbitDistance.HasValue)
                {
                    orbit.OrbitDistance = cameraData.Controller.OrbitDistance.Value;
                }

                if (cameraData.Controller.LookAt != null)
                {
                    orbit.FocusOn(ParseVector3(cameraData.Controller.LookAt));
                }
            }
            else
            {
                var freeFly = cameraObj.AddComponent<FreeFlyController>();

                if (cameraData.Controller.MoveSpeed.HasValue)
                {
                    freeFly.MoveSpeed = cameraData.Controller.MoveSpeed.Value;
                }

                if (cameraData.Controller.LookSensitivity.HasValue)
                {
                    freeFly.LookSensitivity = cameraData.Controller.LookSensitivity.Value;
                }

                if (cameraData.Controller.LookAt != null)
                {
                    freeFly.LookAt(ParseVector3(cameraData.Controller.LookAt));
                }
            }
        }
    }

    private void LoadCameras(List<CameraJson>? camerasData)
    {
        if (camerasData == null)
        {
            return;
        }

        foreach (var cameraData in camerasData)
        {
            var cameraObj = CreateObject(cameraData.Name ?? "Camera");

            if (cameraData.Position != null)
            {
                cameraObj.LocalPosition = ParseVector3(cameraData.Position);
            }

            if (cameraData.Rotation != null)
            {
                cameraObj.LocalRotation = ParseRotation(cameraData.Rotation);
            }

            var cameraComponent = cameraObj.AddComponent<CameraComponent>();

            if (cameraData.FieldOfView.HasValue)
            {
                cameraComponent.FieldOfView = cameraData.FieldOfView.Value;
            }

            if (cameraData.NearPlane.HasValue)
            {
                cameraComponent.NearPlane = cameraData.NearPlane.Value;
            }

            if (cameraData.FarPlane.HasValue)
            {
                cameraComponent.FarPlane = cameraData.FarPlane.Value;
            }

            if (!string.IsNullOrEmpty(cameraData.ProjectionType))
            {
                cameraComponent.ProjectionType = cameraData.ProjectionType.ToLowerInvariant() switch
                {
                    "orthographic" => ProjectionType.Orthographic,
                    _ => ProjectionType.Perspective
                };
            }

            if (cameraData.OrthographicSize.HasValue)
            {
                cameraComponent.OrthographicSize = cameraData.OrthographicSize.Value;
            }

            if (cameraData.Priority.HasValue)
            {
                cameraComponent.Priority = cameraData.Priority.Value;
            }

            if (cameraData.IsActive.HasValue)
            {
                cameraComponent.IsActiveCamera = cameraData.IsActive.Value;
            }

            if (cameraData.Controller != null)
            {
                var controllerType = cameraData.Controller.Type?.ToLowerInvariant() ?? "freefly";

                if (controllerType == "orbit")
                {
                    var orbit = cameraObj.AddComponent<OrbitController>();

                    if (cameraData.Controller.LookSensitivity.HasValue)
                    {
                        orbit.LookSensitivity = cameraData.Controller.LookSensitivity.Value;
                    }

                    if (cameraData.Controller.OrbitTarget != null)
                    {
                        orbit.OrbitTarget = ParseVector3(cameraData.Controller.OrbitTarget);
                    }

                    if (cameraData.Controller.OrbitDistance.HasValue)
                    {
                        orbit.OrbitDistance = cameraData.Controller.OrbitDistance.Value;
                    }

                    if (cameraData.Controller.LookAt != null)
                    {
                        orbit.FocusOn(ParseVector3(cameraData.Controller.LookAt));
                    }
                }
                else
                {
                    var freeFly = cameraObj.AddComponent<FreeFlyController>();

                    if (cameraData.Controller.MoveSpeed.HasValue)
                    {
                        freeFly.MoveSpeed = cameraData.Controller.MoveSpeed.Value;
                    }

                    if (cameraData.Controller.LookSensitivity.HasValue)
                    {
                        freeFly.LookSensitivity = cameraData.Controller.LookSensitivity.Value;
                    }

                    if (cameraData.Controller.LookAt != null)
                    {
                        freeFly.LookAt(ParseVector3(cameraData.Controller.LookAt));
                    }
                }
            }
        }
    }

    private void LoadLights(List<LightJson>? lights)
    {
        if (lights == null)
        {
            return;
        }

        foreach (var lightData in lights)
        {
            switch (lightData.Type)
            {
                case LightType.Directional:
                    LoadDirectionalLight(lightData);
                    break;
                case LightType.Point:
                    LoadPointLight(lightData);
                    break;
                case LightType.Spot:
                    LoadSpotLight(lightData);
                    break;
            }
        }
    }

    private void LoadDirectionalLight(LightJson data)
    {
        var light = CreateObject<DirectionalLight>(data.Name ?? "DirectionalLight");

        if (data.Direction != null)
        {
            light.LookAt(Vector3.Normalize(ParseVector3(data.Direction)));
        }

        if (data.Color != null)
        {
            light.Color = ParseVector3(data.Color);
        }

        light.Intensity = data.Intensity;
        light.CastsShadows = data.CastsShadows;
    }

    private void LoadPointLight(LightJson data)
    {
        var light = CreateObject<PointLight>(data.Name ?? "PointLight");

        if (data.Position != null)
        {
            light.LocalPosition = ParseVector3(data.Position);
        }

        if (data.Color != null)
        {
            light.Color = ParseVector3(data.Color);
        }

        light.Intensity = data.Intensity;

        if (data.Range.HasValue)
        {
            light.Range = data.Range.Value;
        }

        light.CastsShadows = data.CastsShadows;
    }

    private void LoadSpotLight(LightJson data)
    {
        var light = CreateObject<SpotLight>(data.Name ?? "SpotLight");

        if (data.Position != null)
        {
            light.LocalPosition = ParseVector3(data.Position);
        }

        if (data.Direction != null)
        {
            light.LookAt(Vector3.Normalize(ParseVector3(data.Direction)));
        }

        if (data.Color != null)
        {
            light.Color = ParseVector3(data.Color);
        }

        light.Intensity = data.Intensity;

        if (data.Range.HasValue)
        {
            light.Range = data.Range.Value;
        }

        if (data.InnerAngle.HasValue)
        {
            light.InnerConeAngle = data.InnerAngle.Value;
        }

        if (data.OuterAngle.HasValue)
        {
            light.OuterConeAngle = data.OuterAngle.Value;
        }

        light.CastsShadows = data.CastsShadows;
    }

    private void LoadObjects(List<GameObjectJson>? objects)
    {
        if (objects == null)
        {
            return;
        }

        foreach (var objData in objects)
        {
            var obj = CreateGameObject(objData);
            Add(obj);
        }
    }

    private GameObject CreateGameObject(GameObjectJson data)
    {
        var obj = new GameObject();

        if (!string.IsNullOrEmpty(data.Name))
        {
            obj.Name = data.Name;
        }

        if (!string.IsNullOrEmpty(data.Tag))
        {
            obj.Tag = data.Tag;
        }

        if (data.Active.HasValue)
        {
            obj.IsActive = data.Active.Value;
        }

        if (data.Position != null)
        {
            obj.LocalPosition = ParseVector3(data.Position);
        }

        if (data.Rotation != null)
        {
            obj.LocalRotation = ParseRotation(data.Rotation);
        }

        if (data.Scale != null)
        {
            obj.LocalScale = ParseVector3(data.Scale);
        }

        if (data.Components != null)
        {
            foreach (var compData in data.Components)
            {
                AddComponent(obj, compData);
            }
        }

        if (data.Children != null)
        {
            foreach (var childData in data.Children)
            {
                var child = CreateGameObject(childData);
                obj.AddChild(child);
            }
        }

        return obj;
    }

    private void AddComponent(GameObject obj, ComponentJson data)
    {
        if (GetShortTypeName(data.Type).Equals("material", StringComparison.OrdinalIgnoreCase))
        {
            var matComp = obj.AddComponent<MaterialComponent>();
            if (data.Properties != null)
            {
                foreach (var (key, value) in data.Properties)
                {
                    if (key == "tags" && value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var tag in value.EnumerateObject())
                        {
                            if (tag.Value.ValueKind == JsonValueKind.String)
                            {
                                matComp.Tags[tag.Name] = tag.Value.GetString() ?? "";
                            }
                        }
                    }
                    else if (value.ValueKind == JsonValueKind.String)
                    {
                        matComp.Tags[key] = value.GetString() ?? "";
                    }
                }
            }
            return;
        }

        if (ComponentRegistry.TryCreate(data.Type, data.Properties, this, out var component) && component != null)
        {
            obj.AddComponent(component);
            return;
        }

        var typeNameLower = data.Type.ToLowerInvariant();
        if (typeNameLower.Contains('.'))
        {
            typeNameLower = typeNameLower[(typeNameLower.LastIndexOf('.') + 1)..];
        }
        if (typeNameLower.EndsWith("component"))
        {
            typeNameLower = typeNameLower[..^9];
        }

        switch (typeNameLower)
        {
            case "mesh":
                var meshComp = obj.AddComponent<MeshComponent>();
                meshComp.Mesh = ResolveMeshFromProperties(data.Properties);
                break;

            case "rigidbody":
                var shape = ParsePhysicsShape(data.Properties);
                var bodyType = data.Properties.GetEnumOrDefault("bodyType", BodyType.Static);
                var mass = data.Properties.GetSingleOrDefault("mass", 1f);

                var rb = bodyType switch
                {
                    BodyType.Dynamic => Rigidbody.Dynamic(shape, mass),
                    BodyType.Static => Rigidbody.Static(shape),
                    BodyType.Kinematic => Rigidbody.Kinematic(shape),
                    _ => Rigidbody.Static(shape)
                };
                obj.AddComponent(rb);
                break;

            case "surface":
                var surfaceComp = obj.AddComponent<SurfaceComponent>();
                if (data.Properties != null)
                {
                    var albedoRef = data.Properties.GetStringOrDefault("albedo");
                    if (!string.IsNullOrEmpty(albedoRef))
                    {
                        surfaceComp.Albedo = ResolveTexture(albedoRef);
                    }

                    var normalRef = data.Properties.GetStringOrDefault("normal");
                    if (!string.IsNullOrEmpty(normalRef))
                    {
                        surfaceComp.Normal = ResolveTexture(normalRef);
                    }

                    var metallicRef = data.Properties.GetStringOrDefault("metallic");
                    if (!string.IsNullOrEmpty(metallicRef))
                    {
                        surfaceComp.Metallic = ResolveTexture(metallicRef);
                    }

                    var roughnessRef = data.Properties.GetStringOrDefault("roughness");
                    if (!string.IsNullOrEmpty(roughnessRef))
                    {
                        surfaceComp.Roughness = ResolveTexture(roughnessRef);
                    }

                    surfaceComp.MetallicValue = data.Properties.GetSingleOrDefault("metallicValue", 0.0f);
                    surfaceComp.RoughnessValue = data.Properties.GetSingleOrDefault("roughnessValue", 0.5f);

                    var albedoColor = data.Properties.GetFloatArrayOrDefault("albedoColor");
                    if (albedoColor is { Length: >= 4 })
                    {
                        surfaceComp.AlbedoColor = new Vector4(albedoColor[0], albedoColor[1], albedoColor[2], albedoColor[3]);
                    }

                    var emissiveColor = data.Properties.GetFloatArrayOrDefault("emissiveColor");
                    if (emissiveColor is { Length: >= 3 })
                    {
                        surfaceComp.EmissiveColor = new Vector3(emissiveColor[0], emissiveColor[1], emissiveColor[2]);
                    }

                    surfaceComp.EmissiveIntensity = data.Properties.GetSingleOrDefault("emissiveIntensity", 0.0f);

                    var uvScale = data.Properties.GetFloatArrayOrDefault("uvScale");
                    if (uvScale is { Length: >= 2 })
                    {
                        surfaceComp.UVScale = new Vector2(uvScale[0], uvScale[1]);
                    }

                    var uvOffset = data.Properties.GetFloatArrayOrDefault("uvOffset");
                    if (uvOffset is { Length: >= 2 })
                    {
                        surfaceComp.UVOffset = new Vector2(uvOffset[0], uvOffset[1]);
                    }
                }
                break;
        }
    }

    private void LoadSkybox(SkyboxJson? skyboxData)
    {
        if (skyboxData == null)
        {
            return;
        }

        Skybox = new SkyboxData
        {
            Right = LoadSkyboxFace(skyboxData.Right),
            Left = LoadSkyboxFace(skyboxData.Left),
            Up = LoadSkyboxFace(skyboxData.Up),
            Down = LoadSkyboxFace(skyboxData.Down),
            Front = LoadSkyboxFace(skyboxData.Front),
            Back = LoadSkyboxFace(skyboxData.Back),
            RightRef = skyboxData.Right,
            LeftRef = skyboxData.Left,
            UpRef = skyboxData.Up,
            DownRef = skyboxData.Down,
            FrontRef = skyboxData.Front,
            BackRef = skyboxData.Back
        };
    }

    private static Texture2d? LoadSkyboxFace(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var texture = new Texture2d();
        texture.Load(path);
        return texture;
    }

    #region IAssetResolver Implementation

    public Mesh? ResolveMesh(string reference, IReadOnlyDictionary<string, object>? parameters = null)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        if (reference.StartsWith("geometry:"))
        {
            var afterPrefix = reference[9..].ToLowerInvariant();
            var colonIdx = afterPrefix.IndexOf(':');
            var geoType = colonIdx >= 0 ? afterPrefix[..colonIdx] : afterPrefix;

            if (colonIdx >= 0)
            {
                var inlineParams = BuildGeometryParams(geoType, afterPrefix[(colonIdx + 1)..].Split(':'));
                return CreateGeometry(geoType, inlineParams);
            }

            return CreateGeometry(geoType, parameters);
        }

        return AssetPacks.GetMeshByPath(reference);
    }

    public Texture2d? ResolveTexture(string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        return AssetPacks.GetTextureByPath(reference);
    }

    public Skeleton? ResolveSkeleton(string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        return AssetPacks.GetSkeletonByPath(reference);
    }

    public Assets.Animation? ResolveAnimation(string reference, Skeleton? skeleton = null)
    {
        if (string.IsNullOrEmpty(reference) || skeleton == null)
        {
            return null;
        }

        var animData = AssetPacks.GetAnimationDataByPath(reference);
        if (animData == null)
        {
            return null;
        }

        return skeleton.LoadAnimation(animData);
    }

    #endregion

    private Mesh? ResolveMeshFromProperties(IReadOnlyDictionary<string, JsonElement>? properties)
    {
        var meshRef = properties.GetStringOrDefault("mesh");
        if (string.IsNullOrEmpty(meshRef))
        {
            return null;
        }

        if (meshRef.StartsWith("geometry:"))
        {
            var afterPrefix = meshRef[9..].ToLowerInvariant();
            var colonIdx = afterPrefix.IndexOf(':');
            var geoType = colonIdx >= 0 ? afterPrefix[..colonIdx] : afterPrefix;

            // If inline params exist (e.g. "geometry:box:1:1:1"), parse and use them
            if (colonIdx >= 0)
            {
                var inlineParams = BuildGeometryParams(geoType, afterPrefix[(colonIdx + 1)..].Split(':'));
                return CreateGeometry(geoType, inlineParams);
            }

            return geoType switch
            {
                "box" => Assets.Assets.CreateBox(
                    properties.GetSingleOrDefault("width", 1f),
                    properties.GetSingleOrDefault("height", 1f),
                    properties.GetSingleOrDefault("depth", 1f)),
                "sphere" => Assets.Assets.CreateSphere(
                    properties.GetSingleOrDefault("diameter", 1f),
                    properties.GetUInt32OrDefault("tessellation", 16)),
                "cylinder" => Assets.Assets.CreateCylinder(
                    properties.GetSingleOrDefault("diameter", 1f),
                    properties.GetSingleOrDefault("height", 1f),
                    properties.GetUInt32OrDefault("tessellation", 16)),
                "cone" => Assets.Assets.CreateCone(
                    properties.GetSingleOrDefault("diameter", 1f),
                    properties.GetSingleOrDefault("height", 1f),
                    properties.GetUInt32OrDefault("tessellation", 16)),
                "quad" => Assets.Assets.CreateQuad(
                    properties.GetSingleOrDefault("width", 1f),
                    properties.GetSingleOrDefault("height", 1f)),
                "torus" => Assets.Assets.CreateTorus(
                    properties.GetSingleOrDefault("diameter", 1f),
                    properties.GetSingleOrDefault("width", 0.3f),
                    properties.GetUInt32OrDefault("tessellation", 16)),
                _ => throw new NotSupportedException($"Unknown geometry type: {geoType}")
            };
        }

        return AssetPacks.GetMeshByPath(meshRef);
    }

    private static Mesh CreateGeometry(string geoType, IReadOnlyDictionary<string, object>? parameters)
    {
        var width = GetParam(parameters, "width", 1f);
        var height = GetParam(parameters, "height", 1f);
        var depth = GetParam(parameters, "depth", 1f);
        var diameter = GetParam(parameters, "diameter", 1f);
        var tessellation = (uint)GetParam(parameters, "tessellation", 16);

        return geoType switch
        {
            "box" => Assets.Assets.CreateBox(width, height, depth),
            "sphere" => Assets.Assets.CreateSphere(diameter, tessellation),
            "cylinder" => Assets.Assets.CreateCylinder(diameter, height, tessellation),
            "cone" => Assets.Assets.CreateCone(diameter, height, tessellation),
            "quad" => Assets.Assets.CreateQuad(width, height),
            "torus" => Assets.Assets.CreateTorus(diameter, width, tessellation),
            _ => throw new NotSupportedException($"Unknown geometry type: {geoType}")
        };
    }

    private static Dictionary<string, object> BuildGeometryParams(string geoType, string[] values)
    {
        string[] paramNames = geoType switch
        {
            "box" => ["width", "height", "depth"],
            "sphere" => ["diameter", "tessellation"],
            "cylinder" => ["diameter", "height", "tessellation"],
            "cone" => ["diameter", "height", "tessellation"],
            "quad" => ["width", "height"],
            "torus" => ["diameter", "width", "tessellation"],
            _ => []
        };

        var dict = new Dictionary<string, object>();
        for (var i = 0; i < paramNames.Length && i < values.Length; i++)
        {
            if (float.TryParse(values[i], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var val))
                dict[paramNames[i]] = val;
        }

        return dict;
    }

    private static float GetParam(IReadOnlyDictionary<string, object>? parameters, string key, float defaultValue)
    {
        if (parameters == null || !parameters.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        return value switch
        {
            float f => f,
            double d => (float)d,
            int i => i,
            _ => defaultValue
        };
    }

    private static PhysicsShape ParsePhysicsShape(IReadOnlyDictionary<string, JsonElement>? properties)
    {
        var shapeType = properties.GetEnumOrDefault("shape", ShapeType.Box);

        return shapeType switch
        {
            ShapeType.Box => CreateBoxShape(properties),
            ShapeType.Sphere => PhysicsShape.Sphere(properties.GetSingleOrDefault("diameter", 1f)),
            ShapeType.Capsule => PhysicsShape.Capsule(
                properties.GetSingleOrDefault("radius", 0.5f),
                properties.GetSingleOrDefault("length", 1f)),
            ShapeType.Cylinder => PhysicsShape.Cylinder(
                properties.GetSingleOrDefault("diameter", 1f),
                properties.GetSingleOrDefault("height", 1f)),
            _ => PhysicsShape.Box(1f, 1f, 1f)
        };
    }

    private static PhysicsShape CreateBoxShape(IReadOnlyDictionary<string, JsonElement>? properties)
    {
        var sizeArr = properties.GetFloatArrayOrDefault("size");
        if (sizeArr is { Length: >= 3 })
        {
            return PhysicsShape.Box(sizeArr[0], sizeArr[1], sizeArr[2]);
        }

        return PhysicsShape.Box(
            properties.GetSingleOrDefault("width", 1f),
            properties.GetSingleOrDefault("height", 1f),
            properties.GetSingleOrDefault("depth", 1f));
    }

    private static Vector3 ParseVector3(float[] arr)
    {
        if (arr.Length < 3)
        {
            throw new ArgumentException("Vector3 array must have at least 3 elements");
        }

        return new Vector3(arr[0], arr[1], arr[2]);
    }

    private static Quaternion ParseRotation(float[] arr)
    {
        if (arr.Length == 3)
        {
            var euler = new Vector3(
                arr[0] * MathF.PI / 180f,
                arr[1] * MathF.PI / 180f,
                arr[2] * MathF.PI / 180f);
            return Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z);
        }

        if (arr.Length >= 4)
        {
            return new Quaternion(arr[0], arr[1], arr[2], arr[3]);
        }

        return Quaternion.Identity;
    }

    private static string GetShortTypeName(string typeName)
    {
        var result = typeName;
        if (result.Contains('.'))
        {
            result = result[(result.LastIndexOf('.') + 1)..];
        }
        if (result.EndsWith("Component", StringComparison.OrdinalIgnoreCase))
        {
            result = result[..^9];
        }
        return result;
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}
