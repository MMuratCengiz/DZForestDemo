using System.Numerics;
using System.Text.Json;
using NiziKit.Assets;
using NiziKit.Assets.Serde;
using NiziKit.AssetPacks;
using NiziKit.Components;
using NiziKit.ContentPipeline;
using NiziKit.Graphics;
using NiziKit.Light;
using NiziKit.Physics;

namespace NiziKit.Core;

public class JsonScene(string jsonPath) : Scene(Path.GetFileNameWithoutExtension(jsonPath)), IAssetResolver
{
    private readonly List<AssetPack> _loadedPacks = new();

    public override void Load()
    {
        SourcePath = jsonPath;
        var json = Content.ReadText(jsonPath);
        var sceneData = SceneJson.FromJson(json);

        Name = sceneData.Name;

        LoadAssetPacks(sceneData.AssetPacks);
        LoadCamera(sceneData.Camera);
        LoadCameras(sceneData.Cameras);
        LoadLights(sceneData.Lights);
        LoadObjects(sceneData.Objects);
    }

    private void LoadAssetPacks(List<string>? packs)
    {
        if (packs == null)
        {
            return;
        }

        foreach (var packPath in packs)
        {
            if (!AssetPacks.AssetPacks.IsLoaded(packPath))
            {
                var pack = AssetPack.Load(packPath);
                _loadedPacks.Add(pack);
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
        if (ComponentRegistry.TryCreate(data.Type, data.Properties, this, out var component) && component != null)
        {
            obj.AddComponent(component);
            return;
        }

        switch (data.Type.ToLowerInvariant())
        {
            case "mesh":
                var meshComp = obj.AddComponent<MeshComponent>();
                var meshRef = data.Properties?.GetStringOrDefault("mesh");
                meshComp.Mesh = ResolveMeshFromProperties(data.Properties);
                meshComp.MeshRef = meshRef;
                break;

            case "material":
                var matComp = obj.AddComponent<MaterialComponent>();
                var materialRef = data.Properties?.GetStringOrDefault("material");
                if (!string.IsNullOrEmpty(materialRef))
                {
                    matComp.Material = ResolveMaterial(materialRef);
                    matComp.MaterialRef = materialRef;
                }
                break;

            case "rigidbody":
                var shape = ParsePhysicsShape(data.Properties);
                var bodyType = data.Properties.GetEnumOrDefault("bodyType", BodyType.Static);
                var mass = data.Properties.GetSingleOrDefault("mass", 1f);

                var rb = bodyType switch
                {
                    BodyType.Dynamic => RigidbodyComponent.Dynamic(shape, mass),
                    BodyType.Static => RigidbodyComponent.Static(shape),
                    BodyType.Kinematic => RigidbodyComponent.Kinematic(shape),
                    _ => RigidbodyComponent.Static(shape)
                };
                obj.AddComponent(rb);
                break;
        }
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
            var geoType = reference.Substring(9).ToLowerInvariant();
            return CreateGeometry(geoType, parameters);
        }

        return ResolvePackAsset(reference, (packName, assetName) =>
        {
            var (modelName, meshSelector) = ParseMeshSelector(assetName);
            var model = AssetPacks.AssetPacks.GetModel(packName, modelName);
            return GetMeshFromModel(model, meshSelector);
        });
    }

    public Material? ResolveMaterial(string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        return ResolvePackAsset(reference, AssetPacks.AssetPacks.GetMaterial);
    }

    public Texture2d? ResolveTexture(string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        return ResolvePackAsset(reference, AssetPacks.AssetPacks.GetTexture);
    }

    public GpuShader? ResolveShader(string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        return ResolvePackAsset(reference, AssetPacks.AssetPacks.GetShader);
    }

    public Skeleton? ResolveSkeleton(string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        return ResolvePackAsset(reference, (packName, modelName) =>
        {
            var model = AssetPacks.AssetPacks.GetModel(packName, modelName);
            return model.Skeleton ?? throw new InvalidOperationException($"Model '{modelName}' does not have a skeleton");
        });
    }

    public Assets.Animation? ResolveAnimation(string reference)
    {
        if (string.IsNullOrEmpty(reference))
        {
            return null;
        }

        return ResolvePackAsset(reference, (packName, assetName) =>
        {
            var (modelName, animationSelector) = ParseMeshSelector(assetName);
            var model = AssetPacks.AssetPacks.GetModel(packName, modelName);
            var skeleton = model.Skeleton ?? throw new InvalidOperationException($"Model '{modelName}' does not have a skeleton");
            return GetAnimationFromSkeleton(skeleton, animationSelector);
        });
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
            var geoType = meshRef.Substring(9).ToLowerInvariant();
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

        return ResolvePackAsset(meshRef, (packName, assetName) =>
        {
            var (modelName, meshSelector) = ParseMeshSelector(assetName);
            var model = AssetPacks.AssetPacks.GetModel(packName, modelName);
            return GetMeshFromModel(model, meshSelector);
        });
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

    private static (string modelName, string? meshSelector) ParseMeshSelector(string assetReference)
    {
        var slashIndex = assetReference.IndexOf('/');
        if (slashIndex > 0)
        {
            return (assetReference.Substring(0, slashIndex), assetReference.Substring(slashIndex + 1));
        }
        return (assetReference, null);
    }

    private static Mesh GetMeshFromModel(Model model, string? meshSelector)
    {
        if (string.IsNullOrEmpty(meshSelector))
        {
            return model.Meshes[0];
        }

        if (int.TryParse(meshSelector, out var index))
        {
            if (index < 0 || index >= model.Meshes.Count)
            {
                throw new IndexOutOfRangeException($"Mesh index {index} is out of range. Model has {model.Meshes.Count} meshes.");
            }
            return model.Meshes[index];
        }

        var mesh = model.Meshes.FirstOrDefault(m => m.Name == meshSelector);
        if (mesh == null)
        {
            throw new KeyNotFoundException($"Mesh '{meshSelector}' not found in model. Available meshes: {string.Join(", ", model.Meshes.Select(m => m.Name))}");
        }
        return mesh;
    }

    private static Assets.Animation GetAnimationFromSkeleton(Skeleton skeleton, string? animationSelector)
    {
        if (string.IsNullOrEmpty(animationSelector))
        {
            return skeleton.GetAnimation(0);
        }

        if (uint.TryParse(animationSelector, out var index))
        {
            return skeleton.GetAnimation(index);
        }

        return skeleton.GetAnimation(animationSelector);
    }

    private static T ResolvePackAsset<T>(string reference, Func<string, string, T> resolver)
    {
        var colonIndex = reference.IndexOf(':');
        if (colonIndex > 0)
        {
            var packName = reference.Substring(0, colonIndex);
            var assetName = reference.Substring(colonIndex + 1);
            return resolver(packName, assetName);
        }
        throw new InvalidOperationException($"Invalid asset reference: {reference}. Expected format: 'packName:assetName'");
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
        if (sizeArr != null && sizeArr.Length >= 3)
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

    public override void Dispose()
    {
        base.Dispose();
    }
}
