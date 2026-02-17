using System.Numerics;
using System.Text.Json;
using NiziKit.Animation;
using NiziKit.Assets.Serde;
using NiziKit.Components;
using NiziKit.ContentPipeline;
using NiziKit.Core;
using NiziKit.Light;
using NiziKit.Physics;

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

    private NiziComponent? CloneComponent(NiziComponent component)
    {
        var type = component.GetType();

        if (Activator.CreateInstance(type) is NiziComponent newComponent)
        {
            foreach (var prop in type.GetProperties())
            {
                if (prop is { CanRead: true, CanWrite: true } && prop.Name != "Owner")
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
        var (json, path) = PrepareSceneSave(scene);
        File.WriteAllText(path, json);
    }

    public async Task SaveSceneAsync(Scene scene)
    {
        var (json, path) = PrepareSceneSave(scene);
        await File.WriteAllTextAsync(path, json);
    }

    private (string json, string path) PrepareSceneSave(Scene scene)
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

        return (json, path);
    }

    public SceneJson ConvertToJson(Scene scene)
    {
        var json = new SceneJson
        {
            Name = scene.Name,
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

        if (scene.Skybox is { IsValid: true })
        {
            json.Skybox = new SkyboxJson
            {
                Right = scene.Skybox.RightRef,
                Left = scene.Skybox.LeftRef,
                Up = scene.Skybox.UpRef,
                Down = scene.Skybox.DownRef,
                Front = scene.Skybox.FrontRef,
                Back = scene.Skybox.BackRef
            };
        }

        json.Lights = [];

        foreach (var obj in scene.RootObjects)
        {
            if (HasCameraComponent(obj))
            {
                continue;
            }

            if (IsLightObject(obj))
            {
                var lightJson = ConvertLightToJson(obj);
                if (lightJson != null)
                {
                    json.Lights.Add(lightJson);
                }
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
        return obj is DirectionalLight or PointLight or SpotLight;
    }

    private static LightJson? ConvertLightToJson(GameObject obj)
    {
        if (obj is DirectionalLight directional)
        {
            return new LightJson
            {
                Type = LightType.Directional,
                Name = directional.Name,
                Direction = [directional.Direction.X, directional.Direction.Y, directional.Direction.Z],
                Color = [directional.Color.X, directional.Color.Y, directional.Color.Z],
                Intensity = directional.Intensity,
                CastsShadows = directional.CastsShadows
            };
        }

        if (obj is PointLight point)
        {
            return new LightJson
            {
                Type = LightType.Point,
                Name = point.Name,
                Position = [point.LocalPosition.X, point.LocalPosition.Y, point.LocalPosition.Z],
                Color = [point.Color.X, point.Color.Y, point.Color.Z],
                Intensity = point.Intensity,
                Range = point.Range,
                CastsShadows = point.CastsShadows
            };
        }

        if (obj is SpotLight spot)
        {
            return new LightJson
            {
                Type = LightType.Spot,
                Name = spot.Name,
                Position = [spot.LocalPosition.X, spot.LocalPosition.Y, spot.LocalPosition.Z],
                Direction = [spot.Direction.X, spot.Direction.Y, spot.Direction.Z],
                Color = [spot.Color.X, spot.Color.Y, spot.Color.Z],
                Intensity = spot.Intensity,
                Range = spot.Range,
                InnerAngle = spot.InnerConeAngle,
                OuterAngle = spot.OuterConeAngle,
                CastsShadows = spot.CastsShadows
            };
        }

        return null;
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

    private ComponentJson? ConvertComponentToJson(NiziComponent component)
    {
        var type = component.GetType();
        var typeName = type.FullName ?? type.Name;

        var json = new ComponentJson
        {
            Type = typeName,
            Properties = []
        };

        if (component is MeshComponent meshComp)
        {
            if (meshComp.Mesh != null && !string.IsNullOrEmpty(meshComp.Mesh.AssetPath))
            {
                json.Properties["mesh"] = JsonSerializer.SerializeToElement(meshComp.Mesh.AssetPath);
            }
        }
        else if (component is MaterialComponent matComp)
        {
            if (matComp.Tags.Count > 0)
            {
                json.Properties["tags"] = JsonSerializer.SerializeToElement(matComp.Tags);
            }
        }
        else if (component is SurfaceComponent surfaceComp)
        {
            if (surfaceComp.Albedo != null && !string.IsNullOrEmpty(surfaceComp.Albedo.SourcePath))
            {
                json.Properties["albedo"] = JsonSerializer.SerializeToElement(surfaceComp.Albedo.SourcePath);
            }
            if (surfaceComp.Normal != null && !string.IsNullOrEmpty(surfaceComp.Normal.SourcePath))
            {
                json.Properties["normal"] = JsonSerializer.SerializeToElement(surfaceComp.Normal.SourcePath);
            }
            if (surfaceComp.Metallic != null && !string.IsNullOrEmpty(surfaceComp.Metallic.SourcePath))
            {
                json.Properties["metallic"] = JsonSerializer.SerializeToElement(surfaceComp.Metallic.SourcePath);
            }
            if (surfaceComp.Roughness != null && !string.IsNullOrEmpty(surfaceComp.Roughness.SourcePath))
            {
                json.Properties["roughness"] = JsonSerializer.SerializeToElement(surfaceComp.Roughness.SourcePath);
            }
            if (surfaceComp.MetallicValue != 0.0f)
            {
                json.Properties["metallicValue"] = JsonSerializer.SerializeToElement(surfaceComp.MetallicValue);
            }
            if (surfaceComp.RoughnessValue != 0.5f)
            {
                json.Properties["roughnessValue"] = JsonSerializer.SerializeToElement(surfaceComp.RoughnessValue);
            }
            if (surfaceComp.AlbedoColor != Vector4.One)
            {
                json.Properties["albedoColor"] = JsonSerializer.SerializeToElement(new[] {
                    surfaceComp.AlbedoColor.X, surfaceComp.AlbedoColor.Y,
                    surfaceComp.AlbedoColor.Z, surfaceComp.AlbedoColor.W
                });
            }
            if (surfaceComp.EmissiveColor != Vector3.Zero)
            {
                json.Properties["emissiveColor"] = JsonSerializer.SerializeToElement(new[] {
                    surfaceComp.EmissiveColor.X, surfaceComp.EmissiveColor.Y, surfaceComp.EmissiveColor.Z
                });
            }
            if (surfaceComp.EmissiveIntensity != 0.0f)
            {
                json.Properties["emissiveIntensity"] = JsonSerializer.SerializeToElement(surfaceComp.EmissiveIntensity);
            }
            if (surfaceComp.UVScale != Vector2.One)
            {
                json.Properties["uvScale"] = JsonSerializer.SerializeToElement(new[] {
                    surfaceComp.UVScale.X, surfaceComp.UVScale.Y
                });
            }
            if (surfaceComp.UVOffset != Vector2.Zero)
            {
                json.Properties["uvOffset"] = JsonSerializer.SerializeToElement(new[] {
                    surfaceComp.UVOffset.X, surfaceComp.UVOffset.Y
                });
            }
        }
        else if (component is Animator animComp)
        {
            if (animComp.Skeleton != null && !string.IsNullOrEmpty(animComp.Skeleton.AssetPath))
            {
                json.Properties["skeleton"] = JsonSerializer.SerializeToElement(animComp.Skeleton.AssetPath);
            }
            if (animComp.RetargetSource != null && !string.IsNullOrEmpty(animComp.RetargetSource.AssetPath))
            {
                json.Properties["retargetSource"] = JsonSerializer.SerializeToElement(animComp.RetargetSource.AssetPath);
            }
            if (!string.IsNullOrEmpty(animComp.DefaultAnimation))
            {
                json.Properties["defaultAnimation"] = JsonSerializer.SerializeToElement(animComp.DefaultAnimation);
            }

            var externalAnimations = animComp.Animations
                .Where(a => a.IsExternal)
                .Select(a => new { name = a.Name, source = a.SourceRef })
                .ToList();

            if (externalAnimations.Count > 0)
            {
                json.Properties["animations"] = JsonSerializer.SerializeToElement(externalAnimations);
            }
        }
        else if (component is Rigidbody rbComp)
        {
            json.Properties["bodyType"] = JsonSerializer.SerializeToElement(rbComp.BodyType.ToString().ToLowerInvariant());
            json.Properties["mass"] = JsonSerializer.SerializeToElement(rbComp.Mass);
            if (rbComp.SpeculativeMargin != 0f)
            {
                json.Properties["speculativeMargin"] = JsonSerializer.SerializeToElement(rbComp.SpeculativeMargin);
            }

            if (rbComp.SleepThreshold != 0f)
            {
                json.Properties["sleepThreshold"] = JsonSerializer.SerializeToElement(rbComp.SleepThreshold);
            }
        }
        else if (component is Collider colliderComp)
        {
            SerializeCollider(json, colliderComp);
        }
        else
        {
            foreach (var prop in type.GetProperties())
            {
                if (prop.CanRead && prop.Name != "Owner" && (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string)))
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

    private static JsonElement SerializePhysicsShape(PhysicsShape shape)
    {
        var obj = new Dictionary<string, object>
        {
            ["type"] = shape.Type.ToString().ToLowerInvariant(),
            ["size"] = new[] { shape.Size.X, shape.Size.Y, shape.Size.Z }
        };
        return JsonSerializer.SerializeToElement(obj);
    }

    private static void SerializeCollider(ComponentJson json, Collider collider)
    {
        if (collider is BoxCollider box)
        {
            json.Properties["size"] = JsonSerializer.SerializeToElement(new[] { box.Size.X, box.Size.Y, box.Size.Z });
            json.Properties["isTrigger"] = JsonSerializer.SerializeToElement(box.IsTrigger);
            if (box.Center != Vector3.Zero)
            {
                json.Properties["center"] = JsonSerializer.SerializeToElement(new[] { box.Center.X, box.Center.Y, box.Center.Z });
            }
        }
        else if (collider is SphereCollider sphere)
        {
            json.Properties["radius"] = JsonSerializer.SerializeToElement(sphere.Radius);
            json.Properties["isTrigger"] = JsonSerializer.SerializeToElement(sphere.IsTrigger);
            if (sphere.Center != Vector3.Zero)
            {
                json.Properties["center"] = JsonSerializer.SerializeToElement(new[] { sphere.Center.X, sphere.Center.Y, sphere.Center.Z });
            }
        }
        else if (collider is CapsuleCollider capsule)
        {
            json.Properties["radius"] = JsonSerializer.SerializeToElement(capsule.Radius);
            json.Properties["height"] = JsonSerializer.SerializeToElement(capsule.Height);
            json.Properties["direction"] = JsonSerializer.SerializeToElement(capsule.Direction.ToString().ToLowerInvariant());
            json.Properties["isTrigger"] = JsonSerializer.SerializeToElement(capsule.IsTrigger);
            if (capsule.Center != Vector3.Zero)
            {
                json.Properties["center"] = JsonSerializer.SerializeToElement(new[] { capsule.Center.X, capsule.Center.Y, capsule.Center.Z });
            }
        }
        else if (collider is CylinderCollider cylinder)
        {
            json.Properties["radius"] = JsonSerializer.SerializeToElement(cylinder.Radius);
            json.Properties["height"] = JsonSerializer.SerializeToElement(cylinder.Height);
            json.Properties["direction"] = JsonSerializer.SerializeToElement(cylinder.Direction.ToString().ToLowerInvariant());
            json.Properties["isTrigger"] = JsonSerializer.SerializeToElement(cylinder.IsTrigger);
            if (cylinder.Center != Vector3.Zero)
            {
                json.Properties["center"] = JsonSerializer.SerializeToElement(new[] { cylinder.Center.X, cylinder.Center.Y, cylinder.Center.Z });
            }
        }
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
