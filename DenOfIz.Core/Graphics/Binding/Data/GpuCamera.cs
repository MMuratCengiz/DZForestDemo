using System.Numerics;
using System.Runtime.InteropServices;

namespace DenOfIz.World.Graphics.Binding.Data;

[StructLayout(LayoutKind.Sequential)]
public struct GpuCamera
{
    public Matrix4x4 View;
    public Matrix4x4 Projection;
    public Matrix4x4 ViewProjection;
    public Matrix4x4 InverseViewProjection;
    public Vector3 CameraPosition;
    public float Time;
    public Vector3 CameraForward;
    public float DeltaTime;
    public Vector2 ScreenSize;
    public float NearPlane;
    public float FarPlane;
}