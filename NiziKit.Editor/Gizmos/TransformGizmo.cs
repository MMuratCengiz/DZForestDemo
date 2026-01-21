using System.Numerics;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Physics;

namespace NiziKit.Editor.Gizmos;

public enum GizmoMode
{
    Translate,
    Rotate,
    Scale
}

public enum GizmoAxis
{
    None,
    X,
    Y,
    Z,
    XY,
    XZ,
    YZ,
    All
}

public enum GizmoSpace
{
    Local,
    World
}

public class TransformGizmo
{
    private const float AxisLength = 1.5f;
    private const float AxisHitRadius = 0.12f;
    private const float PlaneSize = 0.4f;
    private const float PlaneOffset = 0.3f;
    private const float RotateRingRadius = 1.2f;
    private const float RotateRingThickness = 0.15f;
    private const float ScaleBoxSize = 0.2f;
    private const float CenterBoxSize = 0.25f;
    private const float MinGizmoScale = 0.5f;
    private const float MaxGizmoScale = 5f;

    public GizmoMode Mode { get; set; } = GizmoMode.Translate;
    public GizmoSpace Space { get; set; } = GizmoSpace.Local;
    public GizmoAxis HoveredAxis { get; private set; } = GizmoAxis.None;
    public GizmoAxis ActiveAxis { get; private set; } = GizmoAxis.None;
    public bool IsDragging => ActiveAxis != GizmoAxis.None;

    private GameObject? _target;
    private Vector3 _dragStartPosition;
    private Quaternion _dragStartRotation;
    private Vector3 _dragStartScale;
    private Vector3 _dragStartHitPoint;
    private float _dragStartAngle;
    private Vector3 _dragAxisX;
    private Vector3 _dragAxisY;
    private Vector3 _dragAxisZ;

    public GameObject? Target
    {
        get => _target;
        set
        {
            if (_target != value)
            {
                _target = value;
                ActiveAxis = GizmoAxis.None;
            }
        }
    }

    public float GetGizmoScale(CameraObject camera)
    {
        if (_target == null)
        {
            return 1f;
        }

        var meshComponent = _target.GetComponent<MeshComponent>();
        if (meshComponent?.Mesh != null)
        {
            var bounds = meshComponent.Mesh.Bounds;
            var size = bounds.Max - bounds.Min;
            var maxExtent = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
            var objectScale = _target.LocalScale;
            var avgScale = (objectScale.X + objectScale.Y + objectScale.Z) / 3f;
            var scale = maxExtent * avgScale * 0.6f;
            return Math.Clamp(scale, MinGizmoScale, MaxGizmoScale);
        }

        return 1f;
    }

    public void UpdateHover(Ray ray, CameraObject camera)
    {
        if (_target == null || IsDragging)
        {
            return;
        }

        var scale = GetGizmoScale(camera);
        HoveredAxis = GetHitAxis(ray, scale);
    }

    public bool BeginDrag(Ray ray, CameraObject camera)
    {
        if (_target == null)
        {
            return false;
        }

        var scale = GetGizmoScale(camera);
        var hitAxis = GetHitAxis(ray, scale);

        if (hitAxis == GizmoAxis.None)
        {
            return false;
        }

        ActiveAxis = hitAxis;
        _dragStartPosition = _target.LocalPosition;
        _dragStartRotation = _target.LocalRotation;
        _dragStartScale = _target.LocalScale;

        var rotation = Space == GizmoSpace.Local ? _dragStartRotation : Quaternion.Identity;
        _dragAxisX = Vector3.Transform(Vector3.UnitX, rotation);
        _dragAxisY = Vector3.Transform(Vector3.UnitY, rotation);
        _dragAxisZ = Vector3.Transform(Vector3.UnitZ, rotation);

        var hitPoint = GetAxisPlaneIntersection(ray, hitAxis);
        _dragStartHitPoint = hitPoint;

        if (Mode == GizmoMode.Rotate)
        {
            var toHit = hitPoint - _target.WorldPosition;
            if (toHit.LengthSquared() > 0.0001f)
            {
                toHit = Vector3.Normalize(toHit);
                _dragStartAngle = GetAngleOnAxis(toHit, hitAxis);
            }
            else
            {
                _dragStartAngle = 0;
            }
        }

        return true;
    }

    public void UpdateDrag(Ray ray, CameraObject camera)
    {
        if (_target == null || ActiveAxis == GizmoAxis.None)
        {
            return;
        }

        var currentHitPoint = GetAxisPlaneIntersection(ray, ActiveAxis);

        switch (Mode)
        {
            case GizmoMode.Translate:
                ApplyTranslation(currentHitPoint);
                break;
            case GizmoMode.Rotate:
                ApplyRotation(currentHitPoint);
                break;
            case GizmoMode.Scale:
                ApplyScale(currentHitPoint);
                break;
        }
    }

    public void EndDrag()
    {
        ActiveAxis = GizmoAxis.None;
    }

    public void CancelDrag()
    {
        if (_target == null)
        {
            return;
        }

        _target.LocalPosition = _dragStartPosition;
        _target.LocalRotation = _dragStartRotation;
        _target.LocalScale = _dragStartScale;
        ActiveAxis = GizmoAxis.None;
    }

    private GizmoAxis GetHitAxis(Ray ray, float scale)
    {
        if (_target == null)
        {
            return GizmoAxis.None;
        }

        var origin = _target.WorldPosition;
        var rotation = Space == GizmoSpace.Local ? _target.LocalRotation : Quaternion.Identity;

        var axisX = Vector3.Transform(Vector3.UnitX, rotation);
        var axisY = Vector3.Transform(Vector3.UnitY, rotation);
        var axisZ = Vector3.Transform(Vector3.UnitZ, rotation);

        GizmoAxis closestAxis = GizmoAxis.None;
        float closestDist = float.MaxValue;

        switch (Mode)
        {
            case GizmoMode.Translate:
            {
                if (TryHitPlane(ray, origin, axisX, axisY, scale, out var distXY) && distXY < closestDist)
                {
                    closestDist = distXY;
                    closestAxis = GizmoAxis.XY;
                }
                if (TryHitPlane(ray, origin, axisX, axisZ, scale, out var distXZ) && distXZ < closestDist)
                {
                    closestDist = distXZ;
                    closestAxis = GizmoAxis.XZ;
                }
                if (TryHitPlane(ray, origin, axisY, axisZ, scale, out var distYZ) && distYZ < closestDist)
                {
                    closestDist = distYZ;
                    closestAxis = GizmoAxis.YZ;
                }

                if (TryHitAxisLine(ray, origin, axisX, scale, out var distX) && distX < closestDist)
                {
                    closestDist = distX;
                    closestAxis = GizmoAxis.X;
                }
                if (TryHitAxisLine(ray, origin, axisY, scale, out var distY) && distY < closestDist)
                {
                    closestDist = distY;
                    closestAxis = GizmoAxis.Y;
                }
                if (TryHitAxisLine(ray, origin, axisZ, scale, out var distZ) && distZ < closestDist)
                {
                    closestDist = distZ;
                    closestAxis = GizmoAxis.Z;
                }
                break;
            }
            case GizmoMode.Rotate:
            {
                if (TryHitRing(ray, origin, axisX, scale, out var distX) && distX < closestDist)
                {
                    closestDist = distX;
                    closestAxis = GizmoAxis.X;
                }
                if (TryHitRing(ray, origin, axisY, scale, out var distY) && distY < closestDist)
                {
                    closestDist = distY;
                    closestAxis = GizmoAxis.Y;
                }
                if (TryHitRing(ray, origin, axisZ, scale, out var distZ) && distZ < closestDist)
                {
                    closestDist = distZ;
                    closestAxis = GizmoAxis.Z;
                }
                break;
            }
            case GizmoMode.Scale:
            {
                if (TryHitCenterBox(ray, origin, scale, out var distCenter) && distCenter < closestDist)
                {
                    closestDist = distCenter;
                    closestAxis = GizmoAxis.All;
                }

                if (TryHitAxisLine(ray, origin, axisX, scale, out var distX) && distX < closestDist)
                {
                    closestDist = distX;
                    closestAxis = GizmoAxis.X;
                }
                if (TryHitAxisLine(ray, origin, axisY, scale, out var distY) && distY < closestDist)
                {
                    closestDist = distY;
                    closestAxis = GizmoAxis.Y;
                }
                if (TryHitAxisLine(ray, origin, axisZ, scale, out var distZ) && distZ < closestDist)
                {
                    closestDist = distZ;
                    closestAxis = GizmoAxis.Z;
                }
                break;
            }
        }

        return closestAxis;
    }

    private bool TryHitAxisLine(Ray ray, Vector3 origin, Vector3 axis, float scale, out float distance)
    {
        distance = float.MaxValue;

        var axisStart = origin;
        var axisEnd = origin + axis * AxisLength * scale;
        var hitRadius = AxisHitRadius * scale;

        var rayToLineDistance = RayLineSegmentDistance(ray, axisStart, axisEnd, out var rayT, out var lineT);

        if (rayToLineDistance < hitRadius && lineT > 0.05f)
        {
            distance = rayT;
            return true;
        }

        return false;
    }

    private bool TryHitPlane(Ray ray, Vector3 origin, Vector3 axis1, Vector3 axis2, float scale, out float distance)
    {
        distance = float.MaxValue;

        var planeCenter = origin + (axis1 + axis2) * PlaneOffset * scale;
        var planeNormal = Vector3.Normalize(Vector3.Cross(axis1, axis2));

        if (!RayPlaneIntersection(ray, planeCenter, planeNormal, out var hitPoint, out var hitDist))
        {
            return false;
        }

        var localHit = hitPoint - origin;
        var u = Vector3.Dot(localHit, axis1) / scale;
        var v = Vector3.Dot(localHit, axis2) / scale;

        if (u >= PlaneOffset && u <= PlaneOffset + PlaneSize &&
            v >= PlaneOffset && v <= PlaneOffset + PlaneSize)
        {
            distance = hitDist;
            return true;
        }

        return false;
    }

    private bool TryHitRing(Ray ray, Vector3 origin, Vector3 normal, float scale, out float distance)
    {
        distance = float.MaxValue;

        if (!RayPlaneIntersection(ray, origin, normal, out var hitPoint, out var hitDist))
        {
            return false;
        }

        var toHit = hitPoint - origin;
        var distFromCenter = toHit.Length();
        var ringRadius = RotateRingRadius * scale;
        var thickness = RotateRingThickness * scale;

        if (MathF.Abs(distFromCenter - ringRadius) < thickness)
        {
            distance = hitDist;
            return true;
        }

        return false;
    }

    private bool TryHitCenterBox(Ray ray, Vector3 origin, float scale, out float distance)
    {
        distance = float.MaxValue;
        var boxHalfSize = CenterBoxSize * scale * 0.5f;

        return RayBoxIntersection(ray, origin, boxHalfSize, out distance);
    }

    private Vector3 GetAxisPlaneIntersection(Ray ray, GizmoAxis axis)
    {
        if (_target == null)
        {
            return Vector3.Zero;
        }

        var origin = _target.WorldPosition;

        Vector3 planeNormal;
        switch (axis)
        {
            case GizmoAxis.X:
                planeNormal = MathF.Abs(Vector3.Dot(ray.Direction, _dragAxisY)) >
                              MathF.Abs(Vector3.Dot(ray.Direction, _dragAxisZ)) ? _dragAxisY : _dragAxisZ;
                break;
            case GizmoAxis.Y:
                planeNormal = MathF.Abs(Vector3.Dot(ray.Direction, _dragAxisX)) >
                              MathF.Abs(Vector3.Dot(ray.Direction, _dragAxisZ)) ? _dragAxisX : _dragAxisZ;
                break;
            case GizmoAxis.Z:
                planeNormal = MathF.Abs(Vector3.Dot(ray.Direction, _dragAxisX)) >
                              MathF.Abs(Vector3.Dot(ray.Direction, _dragAxisY)) ? _dragAxisX : _dragAxisY;
                break;
            case GizmoAxis.XY:
                planeNormal = _dragAxisZ;
                break;
            case GizmoAxis.XZ:
                planeNormal = _dragAxisY;
                break;
            case GizmoAxis.YZ:
                planeNormal = _dragAxisX;
                break;
            case GizmoAxis.All:
                planeNormal = Vector3.Normalize(ray.Origin - origin);
                break;
            default:
                return origin;
        }

        if (RayPlaneIntersection(ray, origin, planeNormal, out var hitPoint, out _))
        {
            return hitPoint;
        }

        return origin;
    }

    private void ApplyTranslation(Vector3 currentHitPoint)
    {
        if (_target == null)
        {
            return;
        }

        var delta = currentHitPoint - _dragStartHitPoint;

        Vector3 constrainedDelta;
        switch (ActiveAxis)
        {
            case GizmoAxis.X:
                constrainedDelta = _dragAxisX * Vector3.Dot(delta, _dragAxisX);
                break;
            case GizmoAxis.Y:
                constrainedDelta = _dragAxisY * Vector3.Dot(delta, _dragAxisY);
                break;
            case GizmoAxis.Z:
                constrainedDelta = _dragAxisZ * Vector3.Dot(delta, _dragAxisZ);
                break;
            case GizmoAxis.XY:
                constrainedDelta = _dragAxisX * Vector3.Dot(delta, _dragAxisX) + _dragAxisY * Vector3.Dot(delta, _dragAxisY);
                break;
            case GizmoAxis.XZ:
                constrainedDelta = _dragAxisX * Vector3.Dot(delta, _dragAxisX) + _dragAxisZ * Vector3.Dot(delta, _dragAxisZ);
                break;
            case GizmoAxis.YZ:
                constrainedDelta = _dragAxisY * Vector3.Dot(delta, _dragAxisY) + _dragAxisZ * Vector3.Dot(delta, _dragAxisZ);
                break;
            default:
                constrainedDelta = delta;
                break;
        }

        _target.LocalPosition = _dragStartPosition + constrainedDelta;
    }

    private void ApplyRotation(Vector3 currentHitPoint)
    {
        if (_target == null)
        {
            return;
        }

        var toHit = currentHitPoint - _target.WorldPosition;
        if (toHit.LengthSquared() < 0.0001f)
        {
            return;
        }

        toHit = Vector3.Normalize(toHit);
        var currentAngle = GetAngleOnAxis(toHit, ActiveAxis);
        var deltaAngle = currentAngle - _dragStartAngle;

        Vector3 rotationAxis;
        switch (ActiveAxis)
        {
            case GizmoAxis.X:
                rotationAxis = _dragAxisX;
                break;
            case GizmoAxis.Y:
                rotationAxis = _dragAxisY;
                break;
            case GizmoAxis.Z:
                rotationAxis = _dragAxisZ;
                break;
            default:
                return;
        }

        var deltaRotation = Quaternion.CreateFromAxisAngle(rotationAxis, deltaAngle);
        _target.LocalRotation = deltaRotation * _dragStartRotation;
    }

    private void ApplyScale(Vector3 currentHitPoint)
    {
        if (_target == null)
        {
            return;
        }

        var startDist = Vector3.Distance(_dragStartHitPoint, _target.WorldPosition);
        var currentDist = Vector3.Distance(currentHitPoint, _target.WorldPosition);

        if (startDist < 0.001f)
        {
            return;
        }

        var scaleFactor = currentDist / startDist;
        scaleFactor = Math.Clamp(scaleFactor, 0.01f, 100f);

        switch (ActiveAxis)
        {
            case GizmoAxis.X:
                _target.LocalScale = _dragStartScale with { X = _dragStartScale.X * scaleFactor };
                break;
            case GizmoAxis.Y:
                _target.LocalScale = _dragStartScale with { Y = _dragStartScale.Y * scaleFactor };
                break;
            case GizmoAxis.Z:
                _target.LocalScale = _dragStartScale with { Z = _dragStartScale.Z * scaleFactor };
                break;
            case GizmoAxis.All:
                _target.LocalScale = _dragStartScale * scaleFactor;
                break;
        }
    }

    private float GetAngleOnAxis(Vector3 direction, GizmoAxis axis)
    {
        Vector3 axis1, axis2;
        switch (axis)
        {
            case GizmoAxis.X:
                axis1 = _dragAxisY;
                axis2 = _dragAxisZ;
                break;
            case GizmoAxis.Y:
                axis1 = _dragAxisZ;
                axis2 = _dragAxisX;
                break;
            case GizmoAxis.Z:
                axis1 = _dragAxisX;
                axis2 = _dragAxisY;
                break;
            default:
                return 0;
        }

        var u = Vector3.Dot(direction, axis1);
        var v = Vector3.Dot(direction, axis2);
        return MathF.Atan2(v, u);
    }

    private static float RayLineSegmentDistance(Ray ray, Vector3 lineStart, Vector3 lineEnd, out float rayT, out float lineT)
    {
        var lineDir = lineEnd - lineStart;
        var lineLength = lineDir.Length();
        if (lineLength < 0.0001f)
        {
            rayT = 0;
            lineT = 0;
            return Vector3.Distance(ray.Origin, lineStart);
        }
        lineDir /= lineLength;

        var w0 = ray.Origin - lineStart;
        var a = Vector3.Dot(ray.Direction, ray.Direction);
        var b = Vector3.Dot(ray.Direction, lineDir);
        var c = Vector3.Dot(lineDir, lineDir);
        var d = Vector3.Dot(ray.Direction, w0);
        var e = Vector3.Dot(lineDir, w0);

        var denom = a * c - b * b;

        if (MathF.Abs(denom) < 1e-6f)
        {
            rayT = 0;
            lineT = e / c;
        }
        else
        {
            rayT = (b * e - c * d) / denom;
            lineT = (a * e - b * d) / denom;
        }

        rayT = MathF.Max(0, rayT);
        lineT = Math.Clamp(lineT / lineLength, 0, 1);

        var closestOnRay = ray.Origin + ray.Direction * rayT;
        var closestOnLine = lineStart + (lineEnd - lineStart) * lineT;

        return Vector3.Distance(closestOnRay, closestOnLine);
    }

    private static bool RayPlaneIntersection(Ray ray, Vector3 planePoint, Vector3 planeNormal, out Vector3 hitPoint, out float distance)
    {
        hitPoint = Vector3.Zero;
        distance = 0;

        var denom = Vector3.Dot(planeNormal, ray.Direction);
        if (MathF.Abs(denom) < 1e-6f)
        {
            return false;
        }

        var t = Vector3.Dot(planePoint - ray.Origin, planeNormal) / denom;
        if (t < 0)
        {
            return false;
        }

        hitPoint = ray.Origin + ray.Direction * t;
        distance = t;
        return true;
    }

    private static bool RayBoxIntersection(Ray ray, Vector3 boxCenter, float boxHalfSize, out float distance)
    {
        distance = float.MaxValue;

        var min = boxCenter - new Vector3(boxHalfSize);
        var max = boxCenter + new Vector3(boxHalfSize);

        var invDir = new Vector3(1f / ray.Direction.X, 1f / ray.Direction.Y, 1f / ray.Direction.Z);

        var t1 = (min.X - ray.Origin.X) * invDir.X;
        var t2 = (max.X - ray.Origin.X) * invDir.X;
        var t3 = (min.Y - ray.Origin.Y) * invDir.Y;
        var t4 = (max.Y - ray.Origin.Y) * invDir.Y;
        var t5 = (min.Z - ray.Origin.Z) * invDir.Z;
        var t6 = (max.Z - ray.Origin.Z) * invDir.Z;

        var tmin = MathF.Max(MathF.Max(MathF.Min(t1, t2), MathF.Min(t3, t4)), MathF.Min(t5, t6));
        var tmax = MathF.Min(MathF.Min(MathF.Max(t1, t2), MathF.Max(t3, t4)), MathF.Max(t5, t6));

        if (tmax < 0 || tmin > tmax)
        {
            return false;
        }

        distance = tmin >= 0 ? tmin : tmax;
        return true;
    }

    public static bool RayBoundsIntersection(Ray ray, BoundingBox bounds, Matrix4x4 worldMatrix, out float distance)
    {
        distance = float.MaxValue;

        Matrix4x4.Invert(worldMatrix, out var invWorld);

        var localOrigin = Vector3.Transform(ray.Origin, invWorld);
        var localDir = Vector3.TransformNormal(ray.Direction, invWorld);
        if (localDir.LengthSquared() < 0.0001f)
        {
            return false;
        }
        localDir = Vector3.Normalize(localDir);

        var min = bounds.Min;
        var max = bounds.Max;

        var invDir = new Vector3(
            MathF.Abs(localDir.X) > 1e-6f ? 1f / localDir.X : float.MaxValue,
            MathF.Abs(localDir.Y) > 1e-6f ? 1f / localDir.Y : float.MaxValue,
            MathF.Abs(localDir.Z) > 1e-6f ? 1f / localDir.Z : float.MaxValue
        );

        var t1 = (min.X - localOrigin.X) * invDir.X;
        var t2 = (max.X - localOrigin.X) * invDir.X;
        var t3 = (min.Y - localOrigin.Y) * invDir.Y;
        var t4 = (max.Y - localOrigin.Y) * invDir.Y;
        var t5 = (min.Z - localOrigin.Z) * invDir.Z;
        var t6 = (max.Z - localOrigin.Z) * invDir.Z;

        var tmin = MathF.Max(MathF.Max(MathF.Min(t1, t2), MathF.Min(t3, t4)), MathF.Min(t5, t6));
        var tmax = MathF.Min(MathF.Min(MathF.Max(t1, t2), MathF.Max(t3, t4)), MathF.Max(t5, t6));

        if (tmax < 0 || tmin > tmax)
        {
            return false;
        }

        var localHitT = tmin >= 0 ? tmin : tmax;
        var localHitPoint = localOrigin + localDir * localHitT;
        var worldHitPoint = Vector3.Transform(localHitPoint, worldMatrix);
        distance = Vector3.Distance(ray.Origin, worldHitPoint);
        return true;
    }
}
