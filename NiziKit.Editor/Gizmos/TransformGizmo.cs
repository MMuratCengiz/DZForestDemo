using System.Numerics;
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
    private const float AxisHitRadius = 0.15f;
    private const float PlaneSize = 0.4f;
    private const float PlaneOffset = 0.3f;
    private const float RotateRingRadius = 1.2f;
    private const float RotateRingThickness = 0.1f;
    private const float ScaleBoxSize = 0.15f;
    private const float CenterBoxSize = 0.2f;

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
    private Vector3 _lastDragPoint;
    private float _dragStartAngle;

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

        var distance = Vector3.Distance(camera.WorldPosition, _target.WorldPosition);
        return distance * 0.15f;
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

        var hitPoint = GetAxisPlaneIntersection(ray, hitAxis, scale);
        _dragStartHitPoint = hitPoint;
        _lastDragPoint = hitPoint;

        if (Mode == GizmoMode.Rotate)
        {
            var toHit = Vector3.Normalize(hitPoint - _target.WorldPosition);
            _dragStartAngle = GetAngleOnAxis(toHit, hitAxis);
        }

        return true;
    }

    public void UpdateDrag(Ray ray, CameraObject camera)
    {
        if (_target == null || ActiveAxis == GizmoAxis.None)
        {
            return;
        }

        var scale = GetGizmoScale(camera);
        var currentHitPoint = GetAxisPlaneIntersection(ray, ActiveAxis, scale);

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

        _lastDragPoint = currentHitPoint;
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

                if (TryHitAxis(ray, origin, axisX, scale, out var distX) && distX < closestDist)
                {
                    closestDist = distX;
                    closestAxis = GizmoAxis.X;
                }
                if (TryHitAxis(ray, origin, axisY, scale, out var distY) && distY < closestDist)
                {
                    closestDist = distY;
                    closestAxis = GizmoAxis.Y;
                }
                if (TryHitAxis(ray, origin, axisZ, scale, out var distZ) && distZ < closestDist)
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

                if (TryHitScaleBox(ray, origin, axisX, scale, out var distX) && distX < closestDist)
                {
                    closestDist = distX;
                    closestAxis = GizmoAxis.X;
                }
                if (TryHitScaleBox(ray, origin, axisY, scale, out var distY) && distY < closestDist)
                {
                    closestDist = distY;
                    closestAxis = GizmoAxis.Y;
                }
                if (TryHitScaleBox(ray, origin, axisZ, scale, out var distZ) && distZ < closestDist)
                {
                    closestDist = distZ;
                    closestAxis = GizmoAxis.Z;
                }
                break;
            }
        }

        return closestAxis;
    }

    private bool TryHitAxis(Ray ray, Vector3 origin, Vector3 axis, float scale, out float distance)
    {
        distance = float.MaxValue;

        var axisEnd = origin + axis * AxisLength * scale;
        var closestOnRay = ClosestPointOnRay(ray, origin);
        var closestOnAxis = ClosestPointOnSegment(origin, axisEnd, closestOnRay);

        var dist = Vector3.Distance(closestOnRay, closestOnAxis);
        if (dist < AxisHitRadius * scale)
        {
            distance = Vector3.Distance(ray.Origin, closestOnRay);
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

    private bool TryHitScaleBox(Ray ray, Vector3 origin, Vector3 axis, float scale, out float distance)
    {
        distance = float.MaxValue;

        var boxCenter = origin + axis * AxisLength * scale;
        var boxHalfSize = ScaleBoxSize * scale * 0.5f;

        return RayBoxIntersection(ray, boxCenter, boxHalfSize, out distance);
    }

    private bool TryHitCenterBox(Ray ray, Vector3 origin, float scale, out float distance)
    {
        distance = float.MaxValue;
        var boxHalfSize = CenterBoxSize * scale * 0.5f;

        return RayBoxIntersection(ray, origin, boxHalfSize, out distance);
    }

    private Vector3 GetAxisPlaneIntersection(Ray ray, GizmoAxis axis, float scale)
    {
        if (_target == null)
        {
            return Vector3.Zero;
        }

        var origin = _target.WorldPosition;
        var rotation = Space == GizmoSpace.Local ? _target.LocalRotation : Quaternion.Identity;

        var axisX = Vector3.Transform(Vector3.UnitX, rotation);
        var axisY = Vector3.Transform(Vector3.UnitY, rotation);
        var axisZ = Vector3.Transform(Vector3.UnitZ, rotation);

        Vector3 planeNormal;
        switch (axis)
        {
            case GizmoAxis.X:
                planeNormal = MathF.Abs(Vector3.Dot(ray.Direction, axisY)) >
                              MathF.Abs(Vector3.Dot(ray.Direction, axisZ)) ? axisY : axisZ;
                break;
            case GizmoAxis.Y:
                planeNormal = MathF.Abs(Vector3.Dot(ray.Direction, axisX)) >
                              MathF.Abs(Vector3.Dot(ray.Direction, axisZ)) ? axisX : axisZ;
                break;
            case GizmoAxis.Z:
                planeNormal = MathF.Abs(Vector3.Dot(ray.Direction, axisX)) >
                              MathF.Abs(Vector3.Dot(ray.Direction, axisY)) ? axisX : axisY;
                break;
            case GizmoAxis.XY:
                planeNormal = axisZ;
                break;
            case GizmoAxis.XZ:
                planeNormal = axisY;
                break;
            case GizmoAxis.YZ:
                planeNormal = axisX;
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
        var rotation = Space == GizmoSpace.Local ? _target.LocalRotation : Quaternion.Identity;

        var axisX = Vector3.Transform(Vector3.UnitX, rotation);
        var axisY = Vector3.Transform(Vector3.UnitY, rotation);
        var axisZ = Vector3.Transform(Vector3.UnitZ, rotation);

        Vector3 constrainedDelta;
        switch (ActiveAxis)
        {
            case GizmoAxis.X:
                constrainedDelta = axisX * Vector3.Dot(delta, axisX);
                break;
            case GizmoAxis.Y:
                constrainedDelta = axisY * Vector3.Dot(delta, axisY);
                break;
            case GizmoAxis.Z:
                constrainedDelta = axisZ * Vector3.Dot(delta, axisZ);
                break;
            case GizmoAxis.XY:
                constrainedDelta = axisX * Vector3.Dot(delta, axisX) + axisY * Vector3.Dot(delta, axisY);
                break;
            case GizmoAxis.XZ:
                constrainedDelta = axisX * Vector3.Dot(delta, axisX) + axisZ * Vector3.Dot(delta, axisZ);
                break;
            case GizmoAxis.YZ:
                constrainedDelta = axisY * Vector3.Dot(delta, axisY) + axisZ * Vector3.Dot(delta, axisZ);
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

        var toHit = Vector3.Normalize(currentHitPoint - _target.WorldPosition);
        var currentAngle = GetAngleOnAxis(toHit, ActiveAxis);
        var deltaAngle = currentAngle - _dragStartAngle;

        var rotation = Space == GizmoSpace.Local ? _target.LocalRotation : Quaternion.Identity;
        Vector3 axis;
        switch (ActiveAxis)
        {
            case GizmoAxis.X:
                axis = Vector3.Transform(Vector3.UnitX, rotation);
                break;
            case GizmoAxis.Y:
                axis = Vector3.Transform(Vector3.UnitY, rotation);
                break;
            case GizmoAxis.Z:
                axis = Vector3.Transform(Vector3.UnitZ, rotation);
                break;
            default:
                return;
        }

        var deltaRotation = Quaternion.CreateFromAxisAngle(axis, deltaAngle);
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
        var rotation = Space == GizmoSpace.Local && _target != null ? _target.LocalRotation : Quaternion.Identity;

        Vector3 axis1, axis2;
        switch (axis)
        {
            case GizmoAxis.X:
                axis1 = Vector3.Transform(Vector3.UnitY, rotation);
                axis2 = Vector3.Transform(Vector3.UnitZ, rotation);
                break;
            case GizmoAxis.Y:
                axis1 = Vector3.Transform(Vector3.UnitX, rotation);
                axis2 = Vector3.Transform(Vector3.UnitZ, rotation);
                break;
            case GizmoAxis.Z:
                axis1 = Vector3.Transform(Vector3.UnitX, rotation);
                axis2 = Vector3.Transform(Vector3.UnitY, rotation);
                break;
            default:
                return 0;
        }

        var u = Vector3.Dot(direction, axis1);
        var v = Vector3.Dot(direction, axis2);
        return MathF.Atan2(v, u);
    }

    private static Vector3 ClosestPointOnRay(Ray ray, Vector3 point)
    {
        var t = Vector3.Dot(point - ray.Origin, ray.Direction);
        t = MathF.Max(0, t);
        return ray.Origin + ray.Direction * t;
    }

    private static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 point)
    {
        var ab = b - a;
        var t = Vector3.Dot(point - a, ab) / Vector3.Dot(ab, ab);
        t = Math.Clamp(t, 0, 1);
        return a + ab * t;
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
}
