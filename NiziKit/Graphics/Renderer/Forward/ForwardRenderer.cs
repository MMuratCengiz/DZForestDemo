using System.Diagnostics;
using System.Numerics;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Graphics.Batching;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Binding.Data;
using NiziKit.Graphics.Graph;
using NiziKit.Graphics.Renderer.Common;
using NiziKit.SceneManagement;

namespace NiziKit.Graphics.Renderer.Forward;

public class ForwardRenderer : IRenderer
{
    private readonly GraphicsContext _ctx;
    private readonly RenderGraph _graph;
    private readonly RenderPass[] _passes;
    private readonly PresentPass _presentPass;

    private readonly RenderScene _renderScene;
    private readonly GpuView _gpuView;
    private readonly GpuDrawBatcher _drawBatcher;
    private readonly DefaultMaterial _defaultMaterial;
    private readonly ForwardScenePass _forwardScenePass;

    private readonly Dictionary<int, RenderObjectHandle> _gameObjectToRenderObject = new();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private float _lastFrameTime;
    private float _totalTime;

    public ForwardRenderer(GraphicsContext ctx)
    {
        _ctx = ctx;
        _graph = new RenderGraph(ctx);

        _renderScene = new RenderScene();
        _gpuView = new GpuView(ctx);
        _drawBatcher = new GpuDrawBatcher(ctx);

        _defaultMaterial = new DefaultMaterial();
        _defaultMaterial.Initialize(ctx);

        _forwardScenePass = new ForwardScenePass(ctx, _renderScene, _gpuView, _drawBatcher, _defaultMaterial);
        _passes = [_forwardScenePass];
        _presentPass = new BlittingPresentPass(ctx);
    }

    public void Render(World world)
    {
        var scene = world.CurrentScene;
        if (scene == null)
        {
            return;
        }

        var currentTime = (float)_stopwatch.Elapsed.TotalSeconds;
        var deltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;
        _totalTime = currentTime;

        _renderScene.BeginFrame();
        _drawBatcher.BeginFrame(_graph.FrameIndex);

        SyncSceneToRenderScene(scene, world.Assets);

        if (scene.MainCamera != null)
        {
            _renderScene.SetMainView(CreateRenderView(scene.MainCamera));
        }

        _gpuView.Update(scene, _graph.FrameIndex, deltaTime, _totalTime);

        _renderScene.CommitFrame();

        BuildDrawBatches(world.Assets);

        _forwardScenePass.Assets = world.Assets;

        _graph.Execute(_passes.AsSpan(), _presentPass);
    }

    private void SyncSceneToRenderScene(Scene scene, NiziKit.Assets.Assets assets)
    {
        var processedIds = new HashSet<int>();

        foreach (var rootObject in scene.RootObjects)
        {
            SyncGameObjectRecursive(rootObject, assets, processedIds);
        }

        var toRemove = new List<int>();
        foreach (var (gameObjectId, handle) in _gameObjectToRenderObject)
        {
            if (!processedIds.Contains(gameObjectId))
            {
                _renderScene.Remove(handle);
                toRemove.Add(gameObjectId);
            }
        }

        foreach (var id in toRemove)
        {
            _gameObjectToRenderObject.Remove(id);
        }
    }

    private void SyncGameObjectRecursive(GameObject gameObject, NiziKit.Assets.Assets assets, HashSet<int> processedIds)
    {
        if (!gameObject.IsActive)
        {
            return;
        }

        var meshComponent = gameObject.GetComponent<MeshComponent>();
        if (meshComponent != null && meshComponent.Mesh.IsValid)
        {
            processedIds.Add(gameObject.Id);

            if (_gameObjectToRenderObject.TryGetValue(gameObject.Id, out var existingHandle))
            {
                _renderScene.SetTransform(existingHandle, gameObject.WorldMatrix);
                _renderScene.SetMaterial(existingHandle, meshComponent.Material);
            }
            else
            {
                var handle = _renderScene.Add(new RenderObjectDesc
                {
                    Mesh = meshComponent.Mesh,
                    Transform = gameObject.WorldMatrix,
                    Material = meshComponent.Material,
                    Flags = meshComponent.Flags
                });

                if (handle.IsValid())
                {
                    _gameObjectToRenderObject[gameObject.Id] = handle;
                }
            }
        }

        foreach (var child in gameObject.Children)
        {
            SyncGameObjectRecursive(child, assets, processedIds);
        }
    }

    private void BuildDrawBatches(NiziKit.Assets.Assets assets)
    {
        foreach (var batch in _renderScene.StaticBatches)
        {
            var instances = _renderScene.StaticInstances.Slice(batch.StartIndex, batch.Count);

            Span<GpuInstanceData> gpuInstances = stackalloc GpuInstanceData[batch.Count];
            for (var i = 0; i < batch.Count; i++)
            {
                var inst = instances[i];
                gpuInstances[i] = new GpuInstanceData
                {
                    Model = inst.Transform,
                    BaseColor = inst.BaseColor,
                    Metallic = inst.Metallic,
                    Roughness = inst.Roughness,
                    AmbientOcclusion = inst.AmbientOcclusion,
                    UseAlbedoTexture = inst.AlbedoTextureIndex >= 0 ? 1u : 0u,
                    BoneOffset = 0
                };
            }

            _drawBatcher.AddStaticDraw(batch.Mesh, gpuInstances);
        }

        foreach (var skinned in _renderScene.SkinnedInstances)
        {
            var bones = _renderScene.SkinnedBoneMatrices.Slice(skinned.BoneMatricesOffset, skinned.BoneCount);

            var gpuInstance = new GpuInstanceData
            {
                Model = skinned.Transform,
                BaseColor = skinned.Material.BaseColor,
                Metallic = skinned.Material.Metallic,
                Roughness = skinned.Material.Roughness,
                AmbientOcclusion = skinned.Material.AmbientOcclusion,
                UseAlbedoTexture = skinned.Material.AlbedoTexture.IsValid ? 1u : 0u,
                BoneOffset = 0
            };

            _drawBatcher.AddSkinnedDraw(skinned.Mesh, gpuInstance, bones);
        }
    }

    private static RenderView CreateRenderView(CameraObject camera)
    {
        return new RenderView
        {
            View = camera.ViewMatrix,
            Projection = camera.ProjectionMatrix,
            ViewProjection = camera.ViewProjectionMatrix,
            Position = camera.WorldPosition,
            NearPlane = camera.NearPlane,
            FarPlane = camera.FarPlane,
            FieldOfView = camera.FieldOfView
        };
    }

    public void OnResize(uint width, uint height)
    {
        _graph.Resize(width, height);
    }

    public void Dispose()
    {
        foreach (var pass in _passes)
        {
            pass.Dispose();
        }

        _presentPass.Dispose();
        _graph.Dispose();
        _renderScene.Dispose();
        _gpuView.Dispose();
        _drawBatcher.Dispose();
        _defaultMaterial.Dispose();
    }
}
