using System.Text.Json;
using System.Text.Json.Serialization;

namespace NiziKit.Assets.Serde;

public enum LightType
{
    Directional,
    Point,
    Spot
}

public enum BodyType
{
    Static,
    Dynamic,
    Kinematic
}

public enum ShapeType
{
    Box,
    Sphere,
    Capsule,
    Cylinder
}

public sealed class Vector3Json
{
    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("z")]
    public float Z { get; set; }
}

public sealed class CameraControllerJson
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("lookAt")]
    public float[]? LookAt { get; set; }

    [JsonPropertyName("moveSpeed")]
    public float? MoveSpeed { get; set; }

    [JsonPropertyName("lookSensitivity")]
    public float? LookSensitivity { get; set; }

    [JsonPropertyName("orbitTarget")]
    public float[]? OrbitTarget { get; set; }

    [JsonPropertyName("orbitDistance")]
    public float? OrbitDistance { get; set; }
}

public sealed class CameraJson
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("position")]
    public float[]? Position { get; set; }

    [JsonPropertyName("rotation")]
    public float[]? Rotation { get; set; }

    [JsonPropertyName("fieldOfView")]
    public float? FieldOfView { get; set; }

    [JsonPropertyName("nearPlane")]
    public float? NearPlane { get; set; }

    [JsonPropertyName("farPlane")]
    public float? FarPlane { get; set; }

    [JsonPropertyName("projectionType")]
    public string? ProjectionType { get; set; }

    [JsonPropertyName("orthographicSize")]
    public float? OrthographicSize { get; set; }

    [JsonPropertyName("priority")]
    public int? Priority { get; set; }

    [JsonPropertyName("isActive")]
    public bool? IsActive { get; set; }

    [JsonPropertyName("controller")]
    public CameraControllerJson? Controller { get; set; }
}

public sealed class LightJson
{
    [JsonPropertyName("type")]
    public LightType Type { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("position")]
    public float[]? Position { get; set; }

    [JsonPropertyName("direction")]
    public float[]? Direction { get; set; }

    [JsonPropertyName("color")]
    public float[]? Color { get; set; }

    [JsonPropertyName("intensity")]
    public float Intensity { get; set; } = 1.0f;

    [JsonPropertyName("range")]
    public float? Range { get; set; }

    [JsonPropertyName("castsShadows")]
    public bool CastsShadows { get; set; }

    [JsonPropertyName("innerAngle")]
    public float? InnerAngle { get; set; }

    [JsonPropertyName("outerAngle")]
    public float? OuterAngle { get; set; }
}

public sealed class ComponentJson
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Properties { get; set; }
}

public sealed class GameObjectJson
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    [JsonPropertyName("active")]
    public bool? Active { get; set; }

    [JsonPropertyName("position")]
    public float[]? Position { get; set; }

    [JsonPropertyName("rotation")]
    public float[]? Rotation { get; set; }

    [JsonPropertyName("scale")]
    public float[]? Scale { get; set; }

    [JsonPropertyName("components")]
    public List<ComponentJson>? Components { get; set; }

    [JsonPropertyName("children")]
    public List<GameObjectJson>? Children { get; set; }

    [JsonPropertyName("properties")]
    public JsonElement? Properties { get; set; }
}

public sealed class SceneJson
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("assetPacks")]
    public List<string>? AssetPacks { get; set; }

    [JsonPropertyName("camera")]
    public CameraJson? Camera { get; set; }

    [JsonPropertyName("cameras")]
    public List<CameraJson>? Cameras { get; set; }

    [JsonPropertyName("lights")]
    public List<LightJson>? Lights { get; set; }

    [JsonPropertyName("objects")]
    public List<GameObjectJson>? Objects { get; set; }

    public static SceneJson FromJson(string json)
        => JsonSerializer.Deserialize<SceneJson>(json, NiziJsonSerializationOptions.Default)
           ?? throw new InvalidOperationException("Failed to deserialize scene JSON");

    public static async Task<SceneJson> FromJsonAsync(Stream stream, CancellationToken ct = default)
        => await JsonSerializer.DeserializeAsync<SceneJson>(stream, NiziJsonSerializationOptions.Default, ct)
           ?? throw new InvalidOperationException("Failed to deserialize scene JSON");

    public string ToJson() => JsonSerializer.Serialize(this, NiziJsonSerializationOptions.Default);
}
