using System.Numerics;

namespace Graphics.Binding.Data;

public sealed class GpuBoneMatricesData
{
    public int NumBones { get; }
    public Matrix4x4[] ModelTransforms { get; }
    public Matrix4x4[] FinalBoneMatrices { get; }

    private readonly Matrix4x4[] _inverseBindMatrices;
    private readonly Matrix4x4 _skeletonRootTransform;

    public GpuBoneMatricesData(int numBones, Matrix4x4[] inverseBindMatrices, Matrix4x4 skeletonRootTransform)
    {
        NumBones = numBones;
        ModelTransforms = new Matrix4x4[numBones];
        FinalBoneMatrices = new Matrix4x4[numBones];
        _skeletonRootTransform = skeletonRootTransform;

        _inverseBindMatrices = new Matrix4x4[numBones];
        for (int i = 0; i < numBones; i++)
        {
            _inverseBindMatrices[i] = i < inverseBindMatrices.Length
                ? inverseBindMatrices[i]
                : Matrix4x4.Identity;
            ModelTransforms[i] = Matrix4x4.Identity;
            FinalBoneMatrices[i] = Matrix4x4.Identity;
        }
    }

    public void ComputeFinalMatrices()
    {
        for (int i = 0; i < NumBones; i++)
        {
            FinalBoneMatrices[i] = _inverseBindMatrices[i] * ModelTransforms[i] * _skeletonRootTransform;
        }
    }
}
