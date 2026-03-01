using System.Numerics;
using DenOfIz;
using NiziKit.Application.Timing;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Graphics.Binding;
using NiziKit.Graphics.Resources;

namespace NiziKit.Graphics.Renderer.Renderer2D;

public class Renderer2D : IRenderer
{
    private readonly ViewData _viewData;
    private readonly SpriteShader _spriteShader;
    private readonly Mesh _quadMesh;

    private readonly CycledTexture _sceneColor;
    private readonly CycledTexture _sceneDepth;

    // Object pools — each draw call needs its own target object so GpuBinding
    // creates separate bind groups (with separate GPU buffers) per draw.
    // Reusing a single object would overwrite data before the GPU executes.
    private readonly List<SurfaceComponent> _surfacePool = new(16);
    private readonly List<SpriteBatch> _batchPool = new(16);
    private int _surfacePoolIndex;
    private int _batchPoolIndex;

    private readonly List<(SurfaceComponent surface, SpriteBatch batch)> _drawList = new(64);

    public CameraComponent? Camera
    {
        get => _viewData.Camera;
        set => _viewData.Camera = value;
    }

    public Renderer2D()
    {
        _viewData = new ViewData();

        _sceneColor = CycledTexture.ColorAttachment("SceneColor2D");
        _sceneDepth = CycledTexture.DepthAttachment("SceneDepth2D");

        _spriteShader = new SpriteShader();
        _quadMesh = NiziAssets.CreateQuad(1, 1);

        ShaderHotReload.OnShadersReloaded += OnShadersReloaded;
    }

    public CycledTexture Render(RenderFrame frame)
    {
        var renderWorld2D = World.RenderWorld2D;
        var scene = World.CurrentScene;

        _viewData.Scene = scene;
        _viewData.DeltaTime = Time.DeltaTime;
        _viewData.TotalTime = Time.TotalTime;
        _viewData.ShadowAtlas = null;
        _viewData.ShadowCasters = [];

        var sprites = renderWorld2D.GetSortedSprites();

        // Build draw list: one (surface, batch) pair per material break.
        BuildDrawList(sprites);

        var pass = frame.BeginGraphicsPass();
        pass.SetRenderTarget(0, _sceneColor, LoadOp.Clear);
        pass.SetDepthTarget(_sceneDepth, LoadOp.Clear);
        pass.Begin();

        if (_drawList.Count > 0)
        {
            pass.Bind<ViewBinding>(_viewData);
            pass.BindShader(_spriteShader.Shader);

            SurfaceComponent? boundSurface = null;

            foreach (var (surface, batch) in _drawList)
            {
                if (surface != boundSurface)
                {
                    pass.Bind<SurfaceBinding>(surface);
                    boundSurface = surface;
                }

                pass.Bind<SpriteBatchBinding>(batch);
                pass.DrawMesh(_quadMesh, (uint)batch.Count);
            }
        }

        pass.End();
        return _sceneColor;
    }

    private void BuildDrawList(ReadOnlySpan<Renderable2D> sprites)
    {
        _drawList.Clear();
        _surfacePoolIndex = 0;
        _batchPoolIndex = 0;

        if (sprites.Length == 0)
        {
            return;
        }

        Texture2d? currentTexture = null;
        var currentColor = Vector4.One;
        var currentUVRect = new Vector4(0, 0, 1, 1);
        var currentFlipX = false;
        var currentFlipY = false;
        SpriteBatch? currentBatch = null;
        SurfaceComponent? currentSurface = null;

        for (var i = 0; i < sprites.Length; i++)
        {
            ref readonly var renderable = ref sprites[i];
            var sprite = renderable.Sprite;

            var needNewBatch = currentBatch == null ||
                               currentBatch.Count >= Binding.Data.GpuInstanceArray.MaxInstances ||
                               sprite.Texture != currentTexture ||
                               sprite.Color != currentColor ||
                               sprite.UVRect != currentUVRect ||
                               sprite.FlipX != currentFlipX ||
                               sprite.FlipY != currentFlipY;

            if (needNewBatch)
            {
                if (currentBatch is { Count: > 0 })
                {
                    _drawList.Add((currentSurface!, currentBatch));
                }

                currentTexture = sprite.Texture;
                currentColor = sprite.Color;
                currentUVRect = sprite.UVRect;
                currentFlipX = sprite.FlipX;
                currentFlipY = sprite.FlipY;

                currentSurface = GetPooledSurface();
                ConfigureSurface(currentSurface, currentTexture, currentColor,
                    currentUVRect, currentFlipX, currentFlipY);

                currentBatch = GetPooledBatch();
            }

            currentBatch!.Add(BuildSpriteModel(renderable.Owner, sprite));
        }

        if (currentBatch is { Count: > 0 })
        {
            _drawList.Add((currentSurface!, currentBatch));
        }
    }

    private static void ConfigureSurface(SurfaceComponent surface, Texture2d? texture,
        Vector4 color, Vector4 uvRect, bool flipX, bool flipY)
    {
        surface.Albedo = texture;
        surface.AlbedoColor = color;

        var uvScaleX = uvRect.Z;
        var uvScaleY = uvRect.W;
        if (flipX)
        {
            uvScaleX = -uvScaleX;
        }

        if (flipY)
        {
            uvScaleY = -uvScaleY;
        }

        surface.UVScale = new Vector2(uvScaleX, uvScaleY);
        surface.UVOffset = new Vector2(
            flipX ? uvRect.X + uvRect.Z : uvRect.X,
            flipY ? uvRect.Y + uvRect.W : uvRect.Y);
    }

    private static Matrix4x4 BuildSpriteModel(GameObject owner, SpriteComponent sprite)
    {
        var texture = sprite.Texture;
        var sizeX = sprite.Size.X != 0 ? sprite.Size.X : (texture?.Width ?? 100) / 100f;
        var sizeY = sprite.Size.Y != 0 ? sprite.Size.Y : (texture?.Height ?? 100) / 100f;

        var pivot = sprite.Pivot;
        return Matrix4x4.CreateScale(sizeX, sizeY, 1f)
               * Matrix4x4.CreateTranslation((0.5f - pivot.X) * sizeX, (0.5f - pivot.Y) * sizeY, 0f)
               * owner.WorldMatrix;
    }

    private SurfaceComponent GetPooledSurface()
    {
        if (_surfacePoolIndex >= _surfacePool.Count)
        {
            _surfacePool.Add(new SurfaceComponent());
        }

        return _surfacePool[_surfacePoolIndex++];
    }

    private SpriteBatch GetPooledBatch()
    {
        if (_batchPoolIndex >= _batchPool.Count)
        {
            _batchPool.Add(new SpriteBatch());
        }

        var batch = _batchPool[_batchPoolIndex++];
        batch.Clear();
        return batch;
    }

    private void OnShadersReloaded()
    {
        _spriteShader.Rebuild();
    }

    public void OnResize(uint width, uint height)
    {
    }

    public void Dispose()
    {
        ShaderHotReload.OnShadersReloaded -= OnShadersReloaded;
        _spriteShader.Dispose();
        _sceneColor.Dispose();
        _sceneDepth.Dispose();
    }
}
