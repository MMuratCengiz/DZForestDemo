using System.Numerics;
using System.Text.Json;
using NiziKit.Assets.Serde;
using NiziKit.Components;
using NiziKit.ContentPipeline;
using NiziKit.Core;

namespace NiziKit.Editor.Services;

public class EditorSceneService
{
    public GameObject CloneGameObject(GameObject original)
    {
        var clone = new GameObject(original.Name + " (Clone)")
        {
            Tag = original.Tag,
            IsActive = original.IsActive,
            LocalPosition = original.LocalPosition,
            LocalRotation = original.LocalRotation,
            LocalScale = original.LocalScale
        };

        // Clone components - we'll need to handle this per component type
        foreach (var component in original.Components)
        {
            var clonedComponent = CloneComponent(component);
            if (clonedComponent != null)
            {
                clone.AddComponent(clonedComponent);
            }
        }

        // Clone children recursively
        foreach (var child in original.Children)
        {
            var childClone = CloneGameObject(child);
            clone.AddChild(childClone);
        }

        return clone;
    }

    private IComponent? CloneComponent(IComponent component)
    {
        // For now, create new instances based on type
        // This is a simplified clone that works for basic components
        var type = component.GetType();

        if (Activator.CreateInstance(type) is IComponent newComponent)
        {
            // Copy public properties
            foreach (var prop in type.GetProperties())
            {
                if (prop.CanRead && prop.CanWrite && prop.Name != "Owner")
                {
                    try
                    {
                        var value = prop.GetValue(component);
                        prop.SetValue(newComponent, value);
                    }
                    catch
                    {
                        // Skip properties that can't be copied
                    }
                }
            }
            return newComponent;
        }

        return null;
    }

    public async Task SaveSceneAsync(Scene scene, string filename)
    {
        var sceneJson = ConvertToJson(scene);
        var json = JsonSerializer.Serialize(sceneJson, NiziJsonSerializationOptions.Default);

        var path = Path.Combine(GetScenesDirectory(), filename);
        await File.WriteAllTextAsync(path, json);
    }

    public SceneJson ConvertToJson(Scene scene)
    {
        var json = new SceneJson
        {
            Name = scene.Name,
            AssetPacks = GetLoadedPackNames(),
            Objects = []
        };

        // Convert camera if present
        if (scene.MainCamera != null)
        {
            json.Camera = ConvertCameraToJson(scene.MainCamera);
        }

        // Convert lights
        json.Lights = [];

        // Convert regular objects
        foreach (var obj in scene.RootObjects)
        {
            // Skip camera and light objects
            if (obj is CameraObject || IsLightObject(obj))
            {
                continue;
            }

            json.Objects.Add(ConvertGameObjectToJson(obj));
        }

        return json;
    }

    private static bool IsLightObject(GameObject obj)
    {
        var typeName = obj.GetType().Name;
        return typeName.Contains("Light");
    }

    private CameraJson ConvertCameraToJson(CameraObject camera)
    {
        var euler = QuaternionToEuler(camera.LocalRotation);

        var json = new CameraJson
        {
            Name = camera.Name,
            Position = [camera.LocalPosition.X, camera.LocalPosition.Y, camera.LocalPosition.Z],
            Rotation = [euler.X, euler.Y, euler.Z],
            FieldOfView = camera.FieldOfView,
            NearPlane = camera.NearPlane,
            FarPlane = camera.FarPlane
        };

        var controller = camera.GetComponent<CameraController>();
        if (controller != null)
        {
            json.Controller = new CameraControllerJson
            {
                MoveSpeed = controller.MoveSpeed,
                LookSensitivity = controller.LookSensitivity
            };
        }

        return json;
    }

    private GameObjectJson ConvertGameObjectToJson(GameObject obj)
    {
        var euler = QuaternionToEuler(obj.LocalRotation);

        var json = new GameObjectJson
        {
            Name = obj.Name,
            Tag = obj.Tag,
            Active = obj.IsActive,
            Position = [obj.LocalPosition.X, obj.LocalPosition.Y, obj.LocalPosition.Z],
            Rotation = [euler.X, euler.Y, euler.Z],
            Scale = [obj.LocalScale.X, obj.LocalScale.Y, obj.LocalScale.Z],
            Components = [],
            Children = []
        };

        foreach (var component in obj.Components)
        {
            var compJson = ConvertComponentToJson(component);
            if (compJson != null)
            {
                json.Components.Add(compJson);
            }
        }

        foreach (var child in obj.Children)
        {
            json.Children.Add(ConvertGameObjectToJson(child));
        }

        return json;
    }

    private ComponentJson? ConvertComponentToJson(IComponent component)
    {
        var type = component.GetType();
        var typeName = type.Name.ToLowerInvariant();

        // Remove "Component" suffix
        if (typeName.EndsWith("component"))
        {
            typeName = typeName[..^9];
        }

        var json = new ComponentJson
        {
            Type = typeName,
            Properties = []
        };

        // Handle specific component types
        if (component is MeshComponent meshComp)
        {
            if (meshComp.Mesh != null)
            {
                // Store mesh reference - this is simplified
                json.Properties["mesh"] = JsonSerializer.SerializeToElement(meshComp.Mesh.Name ?? "unknown");
            }
        }
        else if (component is MaterialComponent matComp)
        {
            if (matComp.Material != null)
            {
                json.Properties["material"] = JsonSerializer.SerializeToElement(matComp.Material.Name ?? "unknown");
            }
        }
        else
        {
            // Generic property serialization for other components
            foreach (var prop in type.GetProperties())
            {
                if (prop.CanRead && prop.Name != "Owner" && prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string))
                {
                    try
                    {
                        var value = prop.GetValue(component);
                        if (value != null)
                        {
                            json.Properties[ToCamelCase(prop.Name)] = JsonSerializer.SerializeToElement(value);
                        }
                    }
                    catch
                    {
                        // Skip
                    }
                }
            }
        }

        return json;
    }

    private static Vector3 QuaternionToEuler(Quaternion q)
    {
        var sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        var cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        var roll = MathF.Atan2(sinr_cosp, cosr_cosp);

        var sinp = 2 * (q.W * q.Y - q.Z * q.X);
        var pitch = MathF.Abs(sinp) >= 1 ? MathF.CopySign(MathF.PI / 2, sinp) : MathF.Asin(sinp);

        var siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        var cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        var yaw = MathF.Atan2(siny_cosp, cosy_cosp);

        // Convert to degrees
        return new Vector3(roll, pitch, yaw) * (180f / MathF.PI);
    }

    private static List<string>? GetLoadedPackNames()
    {
        var field = typeof(AssetPacks.AssetPacks).GetField("_packs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (field?.GetValue(null) is Dictionary<string, AssetPacks.AssetPack> packs)
        {
            return packs.Keys.ToList();
        }

        return null;
    }

    private static string GetScenesDirectory()
    {
        // Use the Content system's base path
        var basePath = Path.GetDirectoryName(Content.ResolvePath("dummy")) ?? ".";
        return basePath;
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
        {
            return str;
        }

        return char.ToLowerInvariant(str[0]) + str[1..];
    }
}
