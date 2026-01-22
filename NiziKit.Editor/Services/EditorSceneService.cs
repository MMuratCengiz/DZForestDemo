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

        foreach (var component in original.Components)
        {
            var clonedComponent = CloneComponent(component);
            if (clonedComponent != null)
            {
                clone.AddComponent(clonedComponent);
            }
        }

        foreach (var child in original.Children)
        {
            var childClone = CloneGameObject(child);
            clone.AddChild(childClone);
        }

        return clone;
    }

    private IComponent? CloneComponent(IComponent component)
    {
        var type = component.GetType();

        if (Activator.CreateInstance(type) is IComponent newComponent)
        {
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
                    }
                }
            }
            return newComponent;
        }

        return null;
    }

    public void SaveScene(Scene scene)
    {
        var sceneJson = ConvertToJson(scene);
        var json = JsonSerializer.Serialize(sceneJson, NiziJsonSerializationOptions.Default);

        string path;
        if (!string.IsNullOrEmpty(scene.SourcePath))
        {
            path = Content.ResolvePath(scene.SourcePath);
        }
        else
        {
            var filename = $"{scene.Name}.niziscene.json";
            path = Path.Combine(GetScenesDirectory(), "Scenes", filename);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        }

        File.WriteAllText(path, json);
    }

    public SceneJson ConvertToJson(Scene scene)
    {
        var json = new SceneJson
        {
            Name = scene.Name,
            AssetPacks = GetLoadedPackPaths(),
            Objects = []
        };

        var cameras = scene.GetAllCameras();
        if (cameras.Count > 0)
        {
            json.Cameras = [];
            foreach (var cam in cameras)
            {
                if (cam is CameraComponent cameraComponent)
                {
                    json.Cameras.Add(ConvertCameraComponentToJson(cameraComponent));
                }
            }
        }

        json.Lights = [];

        foreach (var obj in scene.RootObjects)
        {
            if (HasCameraComponent(obj) || IsLightObject(obj))
            {
                continue;
            }

            json.Objects.Add(ConvertGameObjectToJson(obj));
        }

        return json;
    }

    private static bool HasCameraComponent(GameObject obj)
    {
        return obj.GetComponent<CameraComponent>() != null;
    }

    private static bool IsLightObject(GameObject obj)
    {
        var typeName = obj.GetType().Name;
        return typeName.Contains("Light");
    }

    private CameraJson ConvertCameraComponentToJson(CameraComponent camera)
    {
        var owner = camera.Owner;
        if (owner == null)
        {
            return new CameraJson();
        }

        var euler = QuaternionToEuler(owner.LocalRotation);

        var json = new CameraJson
        {
            Name = owner.Name,
            Position = [owner.LocalPosition.X, owner.LocalPosition.Y, owner.LocalPosition.Z],
            Rotation = [euler.X, euler.Y, euler.Z],
            FieldOfView = camera.FieldOfView,
            NearPlane = camera.NearPlane,
            FarPlane = camera.FarPlane,
            ProjectionType = camera.ProjectionType == ProjectionType.Orthographic ? "orthographic" : "perspective",
            OrthographicSize = camera.OrthographicSize,
            Priority = camera.Priority,
            IsActive = camera.IsActiveCamera
        };

        var freeFly = owner.GetComponent<FreeFlyController>();
        if (freeFly != null)
        {
            json.Controller = new CameraControllerJson
            {
                Type = "freefly",
                MoveSpeed = freeFly.MoveSpeed,
                LookSensitivity = freeFly.LookSensitivity
            };
        }

        var orbit = owner.GetComponent<OrbitController>();
        if (orbit != null)
        {
            json.Controller = new CameraControllerJson
            {
                Type = "orbit",
                LookSensitivity = orbit.LookSensitivity,
                OrbitTarget = [orbit.OrbitTarget.X, orbit.OrbitTarget.Y, orbit.OrbitTarget.Z],
                OrbitDistance = orbit.OrbitDistance
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
        if (typeName.EndsWith("component"))
        {
            typeName = typeName[..^9];
        }

        var json = new ComponentJson
        {
            Type = typeName,
            Properties = []
        };

        if (component is MeshComponent meshComp)
        {
            if (!string.IsNullOrEmpty(meshComp.MeshRef))
            {
                json.Properties["mesh"] = JsonSerializer.SerializeToElement(meshComp.MeshRef);
            }
            else if (meshComp.Mesh != null)
            {
                json.Properties["mesh"] = JsonSerializer.SerializeToElement(meshComp.Mesh.Name ?? "unknown");
            }
        }
        else if (component is MaterialComponent matComp)
        {
            if (!string.IsNullOrEmpty(matComp.MaterialRef))
            {
                json.Properties["material"] = JsonSerializer.SerializeToElement(matComp.MaterialRef);
            }
            else if (matComp.Material != null)
            {
                json.Properties["material"] = JsonSerializer.SerializeToElement(matComp.Material.Name ?? "unknown");
            }
        }
        else if (component is AnimatorComponent animComp)
        {
            if (!string.IsNullOrEmpty(animComp.SkeletonRef))
            {
                json.Properties["skeleton"] = JsonSerializer.SerializeToElement(animComp.SkeletonRef);
            }
            else if (animComp.Skeleton != null)
            {
                json.Properties["skeleton"] = JsonSerializer.SerializeToElement(animComp.Skeleton.Name ?? "unknown");
            }
            if (!string.IsNullOrEmpty(animComp.DefaultAnimation))
            {
                json.Properties["defaultAnimation"] = JsonSerializer.SerializeToElement(animComp.DefaultAnimation);
            }
        }
        else
        {
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

        return new Vector3(roll, pitch, yaw) * (180f / MathF.PI);
    }

    private static List<string>? GetLoadedPackPaths()
    {
        var field = typeof(AssetPacks.AssetPacks).GetField("_packs",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        if (field?.GetValue(null) is Dictionary<string, AssetPacks.AssetPack> packs)
        {
            return packs.Values.Select(p => p.SourcePath).Where(p => !string.IsNullOrEmpty(p)).ToList();
        }

        return null;
    }

    private static string GetScenesDirectory()
    {
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
