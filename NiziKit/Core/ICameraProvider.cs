using System.Numerics;

namespace NiziKit.Core;

public interface ICameraProvider
{
    Matrix4x4 ViewMatrix { get; }
    Matrix4x4 ProjectionMatrix { get; }
    Matrix4x4 ViewProjectionMatrix { get; }
    Vector3 WorldPosition { get; }
    Vector3 Forward { get; }
    float NearPlane { get; }
    float FarPlane { get; }
    float AspectRatio { get; set; }
    int Priority { get; }
    bool IsActive { get; }
    void SetAspectRatio(uint width, uint height);
}
