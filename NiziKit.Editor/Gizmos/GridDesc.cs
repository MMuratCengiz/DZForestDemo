using System.Numerics;

namespace NiziKit.Editor.Gizmos;

public class GridDesc
{
    public bool SnapEnabled { get; set; }
    public float PositionSnapIncrement { get; set; } = 1f;
    public float RotationSnapIncrement { get; set; } = 15f;
    public float ScaleSnapIncrement { get; set; } = 0.1f;
    public float ShiftRotationSnapIncrement { get; set; } = 45f;
    public float ShiftPositionSnapIncrement { get; set; } = 0.5f;
    public float ShiftScaleSnapIncrement { get; set; } = 0.25f;

    public static float Snap(float value, float increment)
    {
        if (increment <= 0f)
        {
            return value;
        }
        return MathF.Round(value / increment) * increment;
    }

    public static Vector3 Snap(Vector3 value, float increment)
    {
        if (increment <= 0f)
        {
            return value;
        }
        return new Vector3(
            Snap(value.X, increment),
            Snap(value.Y, increment),
            Snap(value.Z, increment)
        );
    }

    public static float SnapAngle(float radians, float incrementDegrees)
    {
        if (incrementDegrees <= 0f)
        {
            return radians;
        }
        var degrees = radians * (180f / MathF.PI);
        var snapped = MathF.Round(degrees / incrementDegrees) * incrementDegrees;
        return snapped * (MathF.PI / 180f);
    }

    public float GetPositionIncrement(bool shiftHeld)
    {
        if (shiftHeld)
        {
            return ShiftPositionSnapIncrement;
        }
        return SnapEnabled ? PositionSnapIncrement : 0f;
    }

    public float GetRotationIncrement(bool shiftHeld)
    {
        if (shiftHeld)
        {
            return ShiftRotationSnapIncrement;
        }
        return SnapEnabled ? RotationSnapIncrement : 0f;
    }

    public float GetScaleIncrement(bool shiftHeld)
    {
        if (shiftHeld)
        {
            return ShiftScaleSnapIncrement;
        }
        return SnapEnabled ? ScaleSnapIncrement : 0f;
    }
}
