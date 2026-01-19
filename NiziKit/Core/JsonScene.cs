using System.Numerics;
using NiziKit.Assets;
using NiziKit.Assets.Serde;
using NiziKit.AssetPacks;
using NiziKit.Components;
using NiziKit.ContentPipeline;
using NiziKit.Light;
using NiziKit.Physics;

namespace NiziKit.Core;

public class JsonScene : Scene
{
    private readonly string _jsonPath;
    private readonly List<AssetPack> _loadedPacks = new();

    public JsonScene(string jsonPath) : base(Path.GetFileNameWithoutExtension(jsonPath))
    {
        _jsonPath = jsonPath;
    }

    public override void Load()
    {
        var json = Content.ReadText(_jsonPath);
        var sceneData = SceneJson.FromJson(json);

        Name = sceneData.Name;

        LoadAssetPacks(sceneData.AssetPacks);
        LoadCamera(sceneData.Camera);
        LoadLights(sceneData.Lights);
        LoadObjects(sceneData.Objects);
    }

    private void LoadAssetPacks(List<string>? packs)
    {
        if (packs == null) return;

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
        if (cameraData == null) return;

        var camera = CreateObject<CameraObject>(cameraData.Name ?? "Main Camera");

        if (cameraData.Position != null)
            camera.LocalPosition = ParseVector3(cameraData.Position);

        if (cameraData.Rotation != null)
            camera.LocalRotation = ParseRotation(cameraData.Rotation);

        if (cameraData.FieldOfView.HasValue)
            camera.FieldOfView = cameraData.FieldOfView.Value;

        if (cameraData.NearPlane.HasValue)
            camera.NearPlane = cameraData.NearPlane.Value;

        if (cameraData.FarPlane.HasValue)
            camera.FarPlane = cameraData.FarPlane.Value;

        if (cameraData.Controller != null)
        {
            var controller = camera.AddComponent<CameraController>();

            if (cameraData.Controller.MoveSpeed.HasValue)
                controller.MoveSpeed = cameraData.Controller.MoveSpeed.Value;

            if (cameraData.Controller.LookSensitivity.HasValue)
                controller.LookSensitivity = cameraData.Controller.LookSensitivity.Value;

            if (cameraData.Controller.LookAt != null)
                controller.LookAt(ParseVector3(cameraData.Controller.LookAt));
        }

        MainCamera = camera;
    }

    private void LoadLights(List<LightJson>? lights)
    {
        if (lights == null) return;

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
            light.LookAt(Vector3.Normalize(ParseVector3(data.Direction)));

        if (data.Color != null)
            light.Color = ParseVector3(data.Color);

        light.Intensity = data.Intensity;
        light.CastsShadows = data.CastsShadows;
    }

    private void LoadPointLight(LightJson data)
    {
        var light = CreateObject<PointLight>(data.Name ?? "PointLight");

        if (data.Position != null)
            light.LocalPosition = ParseVector3(data.Position);

        if (data.Color != null)
            light.Color = ParseVector3(data.Color);

        light.Intensity = data.Intensity;

        if (data.Range.HasValue)
            light.Range = data.Range.Value;

        light.CastsShadows = data.CastsShadows;
    }

    private void LoadSpotLight(LightJson data)
    {
        var light = CreateObject<SpotLight>(data.Name ?? "SpotLight");

        if (data.Position != null)
            light.LocalPosition = ParseVector3(data.Position);

        if (data.Direction != null)
            light.LookAt(Vector3.Normalize(ParseVector3(data.Direction)));

        if (data.Color != null)
            light.Color = ParseVector3(data.Color);

        light.Intensity = data.Intensity;

        if (data.Range.HasValue)
            light.Range = data.Range.Value;

        if (data.InnerAngle.HasValue)
            light.InnerConeAngle = data.InnerAngle.Value;

        if (data.OuterAngle.HasValue)
            light.OuterConeAngle = data.OuterAngle.Value;

        light.CastsShadows = data.CastsShadows;
    }

    private void LoadObjects(List<GameObjectJson>? objects)
    {
        if (objects == null) return;

        foreach (var objData in objects)
        {
            var obj = CreateGameObject(objData);
            Add(obj);
        }
    }

    private GameObject CreateGameObject(GameObjectJson data)
    {
        GameObject obj;

        if (!string.IsNullOrEmpty(data.Type) && data.Type != "GameObject")
        {
            obj = GameObjectRegistry.Create(data.Type, data.Properties);
        }
        else
        {
            obj = new GameObject();
        }

        if (!string.IsNullOrEmpty(data.Name))
            obj.Name = data.Name;

        if (!string.IsNullOrEmpty(data.Tag))
            obj.Tag = data.Tag;

        if (data.Active.HasValue)
            obj.IsActive = data.Active.Value;

        if (data.Position != null)
            obj.LocalPosition = ParseVector3(data.Position);

        if (data.Rotation != null)
            obj.LocalRotation = ParseRotation(data.Rotation);

        if (data.Scale != null)
            obj.LocalScale = ParseVector3(data.Scale);

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
        switch (data.Type.ToLowerInvariant())
        {
            case "mesh":
                var meshComp = obj.AddComponent<MeshComponent>();
                meshComp.Mesh = ResolveMesh(data);
                break;

            case "material":
                var matComp = obj.AddComponent<MaterialComponent>();
                if (!string.IsNullOrEmpty(data.Material))
                    matComp.Material = ResolveMaterial(data.Material);
                break;

            case "rigidbody":
                var shape = ParsePhysicsShape(data);
                var bodyType = data.BodyType ?? Assets.Serde.BodyType.Static;
                var mass = data.Mass ?? 1f;

                var rb = bodyType switch
                {
                    Assets.Serde.BodyType.Dynamic => RigidbodyComponent.Dynamic(shape, mass),
                    Assets.Serde.BodyType.Static => RigidbodyComponent.Static(shape),
                    Assets.Serde.BodyType.Kinematic => RigidbodyComponent.Kinematic(shape),
                    _ => RigidbodyComponent.Static(shape)
                };
                obj.AddComponent(rb);
                break;
        }
    }

    private Mesh ResolveMesh(ComponentJson data)
    {
        if (string.IsNullOrEmpty(data.Mesh))
            throw new InvalidOperationException("Mesh component requires 'mesh' property");

        var meshRef = data.Mesh;

        if (meshRef.StartsWith("geometry:"))
        {
            var geoType = meshRef.Substring(9).ToLowerInvariant();
            return geoType switch
            {
                "box" => Assets.Assets.CreateBox(
                    data.Width ?? 1f,
                    data.Height ?? 1f,
                    data.Depth ?? 1f),
                "sphere" => Assets.Assets.CreateSphere(
                    data.Diameter ?? 1f,
                    data.Tessellation ?? 16),
                "cylinder" => Assets.Assets.CreateCylinder(
                    data.Diameter ?? 1f,
                    data.Height ?? 1f,
                    data.Tessellation ?? 16),
                "cone" => Assets.Assets.CreateCone(
                    data.Diameter ?? 1f,
                    data.Height ?? 1f,
                    data.Tessellation ?? 16),
                "quad" => Assets.Assets.CreateQuad(
                    data.Width ?? 1f,
                    data.Height ?? 1f),
                "torus" => Assets.Assets.CreateTorus(
                    data.Diameter ?? 1f,
                    data.Width ?? 0.3f,
                    data.Tessellation ?? 16),
                _ => throw new NotSupportedException($"Unknown geometry type: {geoType}")
            };
        }

        if (meshRef.StartsWith("file:"))
        {
            var path = meshRef.Substring(5);
            var model = Assets.Assets.LoadModel(path);
            return model.Meshes[0];
        }

        return ResolvePackAsset(meshRef, (packName, assetName) =>
        {
            var model = AssetPacks.AssetPacks.GetModel(packName, assetName);
            return model.Meshes[0];
        });
    }

    private Material? ResolveMaterial(string reference)
    {
        if (reference.StartsWith("builtin:"))
        {
            return Assets.Assets.GetMaterial(reference.Substring(8));
        }

        if (reference.StartsWith("file:"))
        {
            return Assets.Assets.LoadMaterial(reference.Substring(5));
        }

        return ResolvePackAsset(reference, AssetPacks.AssetPacks.GetMaterial);
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

    private static PhysicsShape ParsePhysicsShape(ComponentJson data)
    {
        var shapeType = data.Shape ?? Assets.Serde.ShapeType.Box;

        return shapeType switch
        {
            Assets.Serde.ShapeType.Box => data.Size != null
                ? PhysicsShape.Box(data.Size[0], data.Size[1], data.Size[2])
                : PhysicsShape.Box(data.Width ?? 1f, data.Height ?? 1f, data.Depth ?? 1f),
            Assets.Serde.ShapeType.Sphere => PhysicsShape.Sphere(data.Diameter ?? 1f),
            Assets.Serde.ShapeType.Capsule => PhysicsShape.Capsule(data.Radius ?? 0.5f, data.Length ?? 1f),
            Assets.Serde.ShapeType.Cylinder => PhysicsShape.Cylinder(data.Diameter ?? 1f, data.Height ?? 1f),
            _ => PhysicsShape.Box(1f, 1f, 1f)
        };
    }

    private static Vector3 ParseVector3(float[] arr)
    {
        if (arr.Length < 3)
            throw new ArgumentException("Vector3 array must have at least 3 elements");
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
