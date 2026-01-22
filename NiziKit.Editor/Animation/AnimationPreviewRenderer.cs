using System.Numerics;
using DenOfIz;
using NiziKit.Animation;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Graphics;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Binding.Data;
using NiziKit.Graphics.Buffers;
using NiziKit.Graphics.Renderer;
using NiziKit.Graphics.Resources;

namespace NiziKit.Editor.Animation;

public class AnimationPreviewRenderer : IDisposable
{
    private const int DefaultPreviewSize = 256;
    private const Format ColorFormat = Format.R8G8B8A8Unorm;

    private readonly RenderFrame _renderFrame;
    private readonly ViewData _viewData;
    private readonly PreviewCamera _previewCamera;
    private readonly PreviewDrawBinding _drawBinding;

    private CycledTexture _colorTarget = null!;
    private CycledTexture _depthTarget = null!;
    private int _width;
    private int _height;
    private bool _disposed;

    private GameObject? _previewTarget;
    private AnimatorComponent? _animatorComponent;
    private Animator? _previewAnimator;
    private string? _currentAnimation;
    private bool _isPlaying;
    private float _playbackSpeed = 1.0f;

    public int Width => _width;
    public int Height => _height;
    public CycledTexture ColorTarget => _colorTarget;
    public Texture CurrentTexture => _colorTarget[GraphicsContext.FrameIndex];
    public bool IsPlaying => _isPlaying;
    public float PlaybackSpeed
    {
        get => _playbackSpeed;
        set => _playbackSpeed = Math.Max(0.01f, value);
    }
    public string? CurrentAnimation => _currentAnimation;

    public AnimationPreviewRenderer(int width = DefaultPreviewSize, int height = DefaultPreviewSize)
    {
        _width = width;
        _height = height;
        _renderFrame = new RenderFrame();
        _viewData = new ViewData();
        _previewCamera = new PreviewCamera();
        _drawBinding = new PreviewDrawBinding();

        CreateRenderTargets();
    }

    private void CreateRenderTargets()
    {
        _colorTarget = CycledTexture.ColorAttachment("AnimationPreviewColor", _width, _height, ColorFormat);
        _depthTarget = CycledTexture.DepthAttachment("AnimationPreviewDepth", _width, _height);
    }

    public void Resize(int width, int height)
    {
        if (_width == width && _height == height) return;

        GraphicsContext.WaitIdle();

        _colorTarget.Dispose();
        _depthTarget.Dispose();

        _width = width;
        _height = height;
        CreateRenderTargets();
        _previewCamera.SetAspectRatio(width, height);
    }

    public void SetPreviewTarget(GameObject? gameObject)
    {
        _previewTarget = gameObject;
        _animatorComponent = gameObject?.GetComponent<AnimatorComponent>();

        if (_animatorComponent != null)
        {
            var skeleton = _animatorComponent.Skeleton;
            if (skeleton != null)
            {
                _previewAnimator = new Animator();
                var controller = CreatePreviewController(skeleton);
                _previewAnimator.Skeleton = skeleton;
                _previewAnimator.Controller = controller;
                _previewAnimator.Initialize();

                PositionCameraForTarget();
            }
        }
        else
        {
            _previewAnimator = null;
        }

        _currentAnimation = null;
        _isPlaying = false;
    }

    private AnimatorController CreatePreviewController(Skeleton skeleton)
    {
        var controller = new AnimatorController { Name = "Preview" };

        for (var i = 0; i < skeleton.AnimationCount; i++)
        {
            var animName = skeleton.AnimationNames[i];
            var state = controller.AddState(animName);
            state.Clip = skeleton.GetAnimation((uint)i);
            state.LoopMode = AnimationLoopMode.Loop;

            if (controller.BaseLayer.DefaultState == null)
            {
                controller.BaseLayer.DefaultState = state;
            }
        }

        return controller;
    }

    private void PositionCameraForTarget()
    {
        if (_previewTarget == null) return;

        var meshComponent = _previewTarget.GetComponent<MeshComponent>();
        var bounds = meshComponent?.Mesh?.Bounds ?? new BoundingBox(Vector3.One * -1, Vector3.One);

        var center = bounds.Center;
        var size = bounds.Size;
        var maxDim = Math.Max(Math.Max(size.X, size.Y), size.Z);

        var distance = maxDim * 2.0f;
        _previewCamera.Position = center + new Vector3(0, maxDim * 0.5f, -distance);
        _previewCamera.Target = center;
        _previewCamera.SetAspectRatio(_width, _height);
    }

    public IReadOnlyList<string> GetAvailableAnimations()
    {
        if (_animatorComponent?.Skeleton == null)
        {
            return Array.Empty<string>();
        }

        return _animatorComponent.Skeleton.AnimationNames;
    }

    public void PlayAnimation(string animationName)
    {
        if (_previewAnimator == null) return;

        _currentAnimation = animationName;
        _previewAnimator.Play(animationName);
        _isPlaying = true;
    }

    public void Pause()
    {
        _isPlaying = false;
    }

    public void Resume()
    {
        _isPlaying = true;
    }

    public void Stop()
    {
        _isPlaying = false;
        if (_previewAnimator != null && _currentAnimation != null)
        {
            _previewAnimator.Play(_currentAnimation);
        }
    }

    public float GetNormalizedTime()
    {
        return _previewAnimator?.GetCurrentStateNormalizedTime() ?? 0f;
    }

    public float GetAnimationDuration()
    {
        var state = _previewAnimator?.GetCurrentState();
        return state?.Clip?.Duration ?? 1f;
    }

    public void SetNormalizedTime(float normalizedTime)
    {
    }

    public void Update(float deltaTime)
    {
        if (_previewAnimator == null || !_isPlaying) return;

        _previewAnimator.Update(deltaTime * _playbackSpeed);
    }

    public void Render()
    {
        if (_previewTarget == null || _previewAnimator == null) return;

        var meshComponent = _previewTarget.GetComponent<MeshComponent>();
        var materialComponent = _previewTarget.GetComponent<MaterialComponent>();

        if (meshComponent?.Mesh == null || materialComponent?.Material == null) return;

        _renderFrame.BeginFrame();

        var pass = _renderFrame.BeginGraphicsPass();
        pass.SetRenderTarget(0, _colorTarget, LoadOp.Clear);
        pass.SetDepthTarget(_depthTarget, LoadOp.Clear);
        pass.Begin();

        _viewData.Camera = _previewCamera.ToCameraComponent();
        _viewData.DeltaTime = 0;
        _viewData.TotalTime = 0;

        pass.Bind<ViewBinding>(_viewData);

        var material = materialComponent.Material;
        var gpuShader = material.GpuShader;
        if (gpuShader != null)
        {
            pass.BindShader(gpuShader);
            pass.Bind<MaterialBinding>(material);

            _drawBinding.UpdateForPreview(_previewTarget, _previewAnimator);
            pass.Bind(_drawBinding.BindGroup);
            pass.DrawMesh(meshComponent.Mesh, 1);
        }

        pass.End();

        _renderFrame.Submit();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        GraphicsContext.WaitIdle();
        _colorTarget.Dispose();
        _depthTarget.Dispose();
        _drawBinding.Dispose();
        _renderFrame.Dispose();
    }
}

internal class PreviewCamera
{
    public Vector3 Position { get; set; } = new(0, 2, 5);
    public Vector3 Target { get; set; } = Vector3.Zero;
    public Vector3 Up { get; set; } = Vector3.UnitY;
    public float FieldOfView { get; set; } = MathF.PI / 4f;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 100f;
    public float AspectRatio { get; private set; } = 1f;
    private int _width = 256;
    private int _height = 256;

    public void SetAspectRatio(int width, int height)
    {
        _width = width;
        _height = height;
        if (height > 0)
        {
            AspectRatio = (float)width / height;
        }
    }

    public Matrix4x4 ViewMatrix => Matrix4x4.CreateLookAtLeftHanded(Position, Target, Up);

    public Matrix4x4 ProjectionMatrix => Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(
        FieldOfView, AspectRatio, NearPlane, FarPlane);

    public CameraComponent ToCameraComponent()
    {
        var tempGo = new GameObject("PreviewCamera");
        tempGo.LocalPosition = Position;
        var forward = Vector3.Normalize(Target - Position);
        tempGo.LocalRotation = QuaternionHelper.LookRotation(forward, Up);

        var camera = tempGo.AddComponent<CameraComponent>();
        camera.FieldOfView = FieldOfView;
        camera.NearPlane = NearPlane;
        camera.FarPlane = FarPlane;
        camera.SetAspectRatio((uint)_width, (uint)_height);

        return camera;
    }
}

internal static class QuaternionHelper
{
    public static Quaternion LookRotation(Vector3 forward, Vector3 up)
    {
        forward = Vector3.Normalize(forward);
        var right = Vector3.Normalize(Vector3.Cross(up, forward));
        up = Vector3.Cross(forward, right);

        var m = new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            up.X, up.Y, up.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);

        return Quaternion.CreateFromRotationMatrix(m);
    }
}

internal class PreviewDrawBinding : IDisposable
{
    private readonly UniformBuffer<GpuInstanceArray> _instanceBuffer;
    private readonly UniformBuffer<GpuBoneTransforms> _boneMatricesBuffer;
    private readonly BindGroup[] _bindGroups;
    private GpuInstanceArray _instanceData;
    private GpuBoneTransforms _boneData;

    public BindGroup BindGroup => _bindGroups[GraphicsContext.FrameIndex];

    public PreviewDrawBinding()
    {
        _instanceBuffer = new UniformBuffer<GpuInstanceArray>(true);
        _boneMatricesBuffer = new UniformBuffer<GpuBoneTransforms>(true);
        _boneData = GpuBoneTransforms.Identity();

        var numFrames = (int)GraphicsContext.NumFrames;
        _bindGroups = new BindGroup[numFrames];

        for (var i = 0; i < numFrames; i++)
        {
            var instanceView = _instanceBuffer[i];
            var boneView = _boneMatricesBuffer[i];

            _bindGroups[i] = GraphicsContext.Device.CreateBindGroup(new BindGroupDesc
            {
                Layout = GraphicsContext.BindGroupLayoutStore.Draw,
            });

            var bg = _bindGroups[i];
            bg.BeginUpdate();
            bg.CbvWithDesc(new BindBufferDesc
            {
                Binding = Graphics.Binding.Layout.GpuDrawLayout.Instances.Binding,
                Resource = instanceView.Buffer,
                ResourceOffset = instanceView.Offset
            });
            bg.CbvWithDesc(new BindBufferDesc
            {
                Binding = Graphics.Binding.Layout.GpuDrawLayout.BoneMatrices.Binding,
                Resource = boneView.Buffer,
                ResourceOffset = boneView.Offset
            });
            bg.EndUpdate();
        }
    }

    public void UpdateForPreview(GameObject gameObject, Animator animator)
    {
        _instanceData.Instances[0] = new GpuInstanceData
        {
            Model = gameObject.WorldMatrix,
            BoneOffset = 0
        };

        if (animator.BoneCount > 0)
        {
            var boneCount = Math.Min(animator.BoneCount, GpuBoneTransforms.MaxBones);
            for (var b = 0; b < boneCount; b++)
            {
                _boneData.Bones[b] = animator.BoneMatrices[b];
            }
        }

        _instanceBuffer.Write(in _instanceData);
        _boneMatricesBuffer.Write(in _boneData);
    }

    public void Dispose()
    {
        foreach (var bg in _bindGroups)
        {
            bg.Dispose();
        }
        _instanceBuffer.Dispose();
        _boneMatricesBuffer.Dispose();
    }
}
