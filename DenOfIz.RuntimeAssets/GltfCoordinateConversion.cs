using System.Numerics;
using System.Runtime.CompilerServices;

namespace RuntimeAssets;

public static class GltfCoordinateConversion
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ConvertPosition(Vector3 v) => new(v.X, v.Y, -v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 ConvertNormal(Vector3 v) => new(v.X, v.Y, -v.Z);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 ConvertTangent(Vector4 v) => new(v.X, v.Y, -v.Z, -v.W);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion ConvertQuaternion(Quaternion q) => new(-q.X, -q.Y, q.Z, q.W);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 ConvertQuaternionVec4(Vector4 v) => new(-v.X, -v.Y, v.Z, v.W);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 TransposeMatrix(Matrix4x4 m) => Matrix4x4.Transpose(m);

    public static Matrix4x4 ConvertMatrixHandedness(Matrix4x4 m)
    {
        return m with
        {
            M13 = -m.M13,
            M23 = -m.M23,
            M31 = -m.M31,
            M32 = -m.M32,
            M34 = -m.M34,
            M43 = -m.M43
        };
    }
    
    public static Matrix4x4 ConvertMatrix(Matrix4x4 m, bool convertHandedness, bool transpose)
    {
        if (convertHandedness)
        {
            m = ConvertMatrixHandedness(m);
        }
        if (transpose)
        {
            m = Matrix4x4.Transpose(m);
        }
        return m;
    }

    public static void ConvertPositionsInPlace(Span<Vector3> positions)
    {
        for (var i = 0; i < positions.Length; i++)
        {
            positions[i] = ConvertPosition(positions[i]);
        }
    }

    public static void ConvertNormalsInPlace(Span<Vector3> normals)
    {
        for (var i = 0; i < normals.Length; i++)
        {
            normals[i] = ConvertNormal(normals[i]);
        }
    }
    public static void ConvertTangentsInPlace(Span<Vector4> tangents)
    {
        for (var i = 0; i < tangents.Length; i++)
        {
            tangents[i] = ConvertTangent(tangents[i]);
        }
    }

    public static void ConvertQuaternionsInPlace(Span<Vector4> quaternions)
    {
        for (var i = 0; i < quaternions.Length; i++)
        {
            quaternions[i] = ConvertQuaternionVec4(quaternions[i]);
        }
    }
    public static void ConvertMatricesInPlace(Span<Matrix4x4> matrices, bool convertHandedness, bool transpose)
    {
        for (var i = 0; i < matrices.Length; i++)
        {
            matrices[i] = ConvertMatrix(matrices[i], convertHandedness, transpose);
        }
    }

    public static void ReverseWindingOrder(Span<uint> indices)
    {
        for (var i = 0; i + 2 < indices.Length; i += 3)
        {
            (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
        }
    }
}