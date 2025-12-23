/*
Den Of Iz - Game/Game Engine
Copyright (c) 2020-2024 Muhammed Murat Cengiz

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DenOfIz;
using Graphics;
using Graphics.Binding;
using Graphics.RenderGraph;
using RuntimeAssets;
using Buffer = DenOfIz.Buffer;

namespace DZForestDemo;

public class RgCommandListDemo : IDisposable
{
    private const int NumObjects = 1000;
    private const uint NumFrames = 3;

    private readonly Window _window;
    private readonly CommandQueue _commandQueue;
    private readonly SwapChain _swapChain;
    private readonly Viewport _viewport;
    private readonly LogicalDevice _logicalDevice;
    private readonly RgCommandList _rgCommandList;
    private readonly ResourceTracking _resourceTracking;
    private readonly StepTimer _stepTimer = new();

    private readonly Shader _shader;

    private readonly Buffer _vertexBuffer;
    private readonly Buffer _indexBuffer;
    private readonly GPUMesh _cubeMesh;
    private readonly uint _indexCount;

    private readonly Buffer[] _instanceBuffers;
    private readonly IntPtr[] _instanceMappedPtrs;
    private readonly InstanceData[] _instances;

    private readonly Buffer[] _frameConstantsBuffers;
    private readonly IntPtr[] _frameConstantsMappedPtrs;

    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachments;
    private float _time;

    public RgCommandListDemo(LogicalDevice logicalDevice, uint width, uint height, string title)
    {
        _logicalDevice = logicalDevice;

        _window = new Window(new WindowDesc
        {
            Width = (int)width,
            Height = (int)height,
            Title = StringView.Create(title)
        });

        var commandQueueDesc = new CommandQueueDesc { QueueType = QueueType.Graphics };
        _commandQueue = logicalDevice.CreateCommandQueue(commandQueueDesc);

        var swapChainDesc = new SwapChainDesc
        {
            AllowTearing = true,
            BackBufferFormat = Format.B8G8R8A8Unorm,
            DepthBufferFormat = Format.D32Float,
            CommandQueue = _commandQueue,
            WindowHandle = _window.GetGraphicsWindowHandle(),
            Width = width,
            Height = height,
            NumBuffers = NumFrames
        };
        _swapChain = logicalDevice.CreateSwapChain(swapChainDesc);
        _viewport = _swapChain.GetViewport();

        _rgCommandList = new RgCommandList(logicalDevice, _commandQueue);

        _window.Show();

        _resourceTracking = new ResourceTracking();
        for (uint i = 0; i < NumFrames; ++i)
        {
            _resourceTracking.TrackTexture(_swapChain.GetRenderTarget(i), QueueType.Graphics);
        }

        _rtAttachments = new PinnedArray<RenderingAttachmentDesc>(1);

        _shader = CreateShader();
        CreateCubeGeometry(out _vertexBuffer, out _indexBuffer, out _indexCount);
        _cubeMesh = new GPUMesh
        {
            IndexType = IndexType.Uint16,
            VertexBuffer =
                new GPUBufferView { Buffer = _vertexBuffer, Offset = 0, NumBytes = _vertexBuffer.NumBytes() },
            IndexBuffer = new GPUBufferView { Buffer = _indexBuffer, Offset = 0, NumBytes = _indexBuffer.NumBytes() },
            NumVertices = 24,
            NumIndices = _indexCount
        };

        _instances = new InstanceData[NumObjects];
        _instanceBuffers = new Buffer[NumFrames];
        _instanceMappedPtrs = new IntPtr[NumFrames];

        var instanceBufferSize = (ulong)(NumObjects * Unsafe.SizeOf<InstanceData>());
        for (var i = 0; i < NumFrames; i++)
        {
            _instanceBuffers[i] = logicalDevice.CreateBuffer(new BufferDesc
            {
                HeapType = HeapType.CpuGpu,
                NumBytes = instanceBufferSize,
                StructureDesc = new StructuredBufferDesc
                {
                    Offset = 0,
                    NumElements = NumObjects,
                    Stride = (ulong)Unsafe.SizeOf<InstanceData>()
                },
                DebugName = StringView.Create($"InstanceBuffer_{i}")
            });
            _instanceMappedPtrs[i] = _instanceBuffers[i].MapMemory();
        }

        _frameConstantsBuffers = new Buffer[NumFrames];
        _frameConstantsMappedPtrs = new IntPtr[NumFrames];

        for (var i = 0; i < NumFrames; i++)
        {
            _frameConstantsBuffers[i] = logicalDevice.CreateBuffer(new BufferDesc
            {
                Usage = (uint)BufferUsageFlagBits.Uniform,
                HeapType = HeapType.CpuGpu,
                NumBytes = (ulong)Unsafe.SizeOf<FrameConstants>(),
                DebugName = StringView.Create($"FrameConstants_{i}")
            });
            _frameConstantsMappedPtrs[i] = _frameConstantsBuffers[i].MapMemory();
        }

        InitializeInstances();
    }

    private Shader CreateShader()
    {
        var shaderLoader = new ShaderLoader();
        var vsSource = shaderLoader.Load("rg_demo_vs.hlsl");
        var psSource = shaderLoader.Load("rg_demo_ps.hlsl");

        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = ShaderStageDescArray.Create([
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("VSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(vsSource)),
                    Stage = (uint)ShaderStageFlagBits.Vertex
                },
                new ShaderStageDesc
                {
                    EntryPoint = StringView.Create("PSMain"),
                    Data = ByteArray.Create(Encoding.UTF8.GetBytes(psSource)),
                    Stage = (uint)ShaderStageFlagBits.Pixel
                }
            ])
        };

        var program = new ShaderProgram(programDesc);
        var reflection = program.Reflect();
        var inputLayout = _logicalDevice.CreateInputLayout(reflection.InputLayout);

        // Build root signature using ShaderRootSignature.Builder
        var rootSignature = new ShaderRootSignature.Builder(_logicalDevice)
            // PerCamera - space 1 (BindingFrequency.PerCamera)
            .AddBinding("FrameConstants", new ResourceBindingDesc
            {
                Binding = 0,
                RegisterSpace = (uint)BindingFrequency.PerCamera,
                Descriptor = (uint)ResourceDescriptorFlagBits.UniformBuffer,
                Stages = (uint)ShaderStageFlagBits.AllGraphics,
                ArraySize = 1
            })
            // PerDraw - space 3 (BindingFrequency.PerDraw)
            .AddBinding("Instances", new ResourceBindingDesc
            {
                Binding = 0,
                RegisterSpace = (uint)BindingFrequency.PerDraw,
                Descriptor = (uint)ResourceDescriptorFlagBits.StructuredBuffer,
                Stages = (uint)ShaderStageFlagBits.AllGraphics,
                ArraySize = 1
            })
            .Build();

        var pipeline = _logicalDevice.CreatePipeline(new PipelineDesc
        {
            InputLayout = inputLayout,
            RootSignature = rootSignature.Instance,
            ShaderProgram = program,
            Graphics = new GraphicsPipelineDesc
            {
                PrimitiveTopology = PrimitiveTopology.Triangle,
                CullMode = CullMode.BackFace,
                RenderTargets = RenderTargetDescArray.Create([
                    new RenderTargetDesc
                    {
                        Format = Format.B8G8R8A8Unorm,
                        Blend = new BlendDesc { RenderTargetWriteMask = 0x0F }
                    }
                ])
            }
        });

        var shader = new Shader(rootSignature);
        shader.AddVariant("default", new ShaderVariant(pipeline, program));
        return shader;
    }

    private void CreateCubeGeometry(out Buffer vertexBuffer, out Buffer indexBuffer, out uint indexCount)
    {
        var vertices = new Vertex[]
        {
            // Front face
            new(new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0, 0, 1), new Vector2(0, 1)),
            new(new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0, 0, 1), new Vector2(1, 1)),
            new(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0, 0, 1), new Vector2(1, 0)),
            new(new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0, 0, 1), new Vector2(0, 0)),

            // Back face
            new(new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, 0, -1), new Vector2(0, 1)),
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0, 0, -1), new Vector2(1, 1)),
            new(new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0, 0, -1), new Vector2(1, 0)),
            new(new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0, 0, -1), new Vector2(0, 0)),

            // Top face
            new(new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0, 1, 0), new Vector2(0, 1)),
            new(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0, 1, 0), new Vector2(1, 1)),
            new(new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0, 1, 0), new Vector2(1, 0)),
            new(new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0, 1, 0), new Vector2(0, 0)),

            // Bottom face
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0, -1, 0), new Vector2(0, 1)),
            new(new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -1, 0), new Vector2(1, 1)),
            new(new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0, -1, 0), new Vector2(1, 0)),
            new(new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0, -1, 0), new Vector2(0, 0)),

            // Right face
            new(new Vector3(0.5f, -0.5f, 0.5f), new Vector3(1, 0, 0), new Vector2(0, 1)),
            new(new Vector3(0.5f, -0.5f, -0.5f), new Vector3(1, 0, 0), new Vector2(1, 1)),
            new(new Vector3(0.5f, 0.5f, -0.5f), new Vector3(1, 0, 0), new Vector2(1, 0)),
            new(new Vector3(0.5f, 0.5f, 0.5f), new Vector3(1, 0, 0), new Vector2(0, 0)),

            // Left face
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-1, 0, 0), new Vector2(0, 1)),
            new(new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-1, 0, 0), new Vector2(1, 1)),
            new(new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-1, 0, 0), new Vector2(1, 0)),
            new(new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-1, 0, 0), new Vector2(0, 0)),
        };

        ushort[] indices =
        [
            0, 1, 2, 0, 2, 3, // Front
            4, 5, 6, 4, 6, 7, // Back
            8, 9, 10, 8, 10, 11, // Top
            12, 13, 14, 12, 14, 15, // Bottom
            16, 17, 18, 16, 18, 19, // Right
            20, 21, 22, 20, 22, 23 // Left
        ];

        indexCount = (uint)indices.Length;

        var vertexSize = (ulong)(vertices.Length * Unsafe.SizeOf<Vertex>());
        var indexSize = (ulong)(indices.Length * sizeof(ushort));

        vertexBuffer = _logicalDevice.CreateBuffer(new BufferDesc
        {
            Usage = (uint)BufferUsageFlagBits.Vertex,
            HeapType = HeapType.CpuGpu,
            NumBytes = vertexSize,
            DebugName = StringView.Create("CubeVertexBuffer")
        });

        indexBuffer = _logicalDevice.CreateBuffer(new BufferDesc
        {
            Usage = (uint)BufferUsageFlagBits.Index,
            HeapType = HeapType.CpuGpu,
            NumBytes = indexSize,
            DebugName = StringView.Create("CubeIndexBuffer")
        });

        var vertexPtr = vertexBuffer.MapMemory();
        unsafe
        {
            fixed (Vertex* src = vertices)
            {
                Unsafe.CopyBlock(vertexPtr.ToPointer(), src, (uint)vertexSize);
            }
        }

        vertexBuffer.UnmapMemory();

        var indexPtr = indexBuffer.MapMemory();
        unsafe
        {
            fixed (ushort* src = indices)
            {
                Unsafe.CopyBlock(indexPtr.ToPointer(), src, (uint)indexSize);
            }
        }

        indexBuffer.UnmapMemory();
    }

    private void InitializeInstances()
    {
        var random = new Random(42);
        var gridSize = (int)Math.Ceiling(Math.Pow(NumObjects, 1.0 / 3.0));
        var spacing = 2.5f;

        for (var i = 0; i < NumObjects; i++)
        {
            var x = i % gridSize;
            var y = (i / gridSize) % gridSize;
            var z = i / (gridSize * gridSize);

            var position = new Vector3(
                (x - gridSize / 2f) * spacing,
                (y - gridSize / 2f) * spacing,
                (z - gridSize / 2f) * spacing
            );

            var rotationX = (float)(random.NextDouble() * Math.PI * 2);
            var rotationY = (float)(random.NextDouble() * Math.PI * 2);

            var hue = (float)random.NextDouble();
            var color = HsvToRgb(hue, 0.8f, 0.9f);

            _instances[i] = new InstanceData
            {
                Model = Matrix4x4.CreateRotationX(rotationX) *
                        Matrix4x4.CreateRotationY(rotationY) *
                        Matrix4x4.CreateTranslation(position),
                Color = new Vector4(color, 1.0f)
            };
        }
    }

    private static Vector3 HsvToRgb(float h, float s, float v)
    {
        var i = (int)(h * 6);
        var f = h * 6 - i;
        var p = v * (1 - s);
        var q = v * (1 - f * s);
        var t = v * (1 - (1 - f) * s);

        return (i % 6) switch
        {
            0 => new Vector3(v, t, p),
            1 => new Vector3(q, v, p),
            2 => new Vector3(p, v, t),
            3 => new Vector3(p, q, v),
            4 => new Vector3(t, p, v),
            _ => new Vector3(v, p, q)
        };
    }

    public bool PollAndRender()
    {
        while (InputSystem.PollEvent(out var ev))
        {
            switch (ev.Type)
            {
                case EventType.Quit:
                    return false;
                case EventType.KeyDown when ev.Key.KeyCode == KeyCode.Escape:
                    return false;
            }
        }

        _stepTimer.Tick();
        _time += (float)_stepTimer.GetDeltaTime();


        Render();
        return true;
    }

    private unsafe void Render()
    {
        var frameIndex = _rgCommandList.NextFrame();

        var image = _swapChain.AcquireNextImage();
        var renderTarget = _swapChain.GetRenderTarget(image);
        
        var aspectRatio = _viewport.Width / _viewport.Height;
        var cameraDistance = 30.0f;
        var cameraPos = new Vector3(
            MathF.Sin(_time * 0.3f) * cameraDistance,
            15.0f,
            MathF.Cos(_time * 0.3f) * cameraDistance
        );

        var view = Matrix4x4.CreateLookAt(cameraPos, Vector3.Zero, Vector3.UnitY);
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4.0f, aspectRatio, 0.1f, 1000.0f);

        var frameConstants = new FrameConstants
        {
            ViewProjection = view * projection,
            CameraPosition = cameraPos,
            Time = _time
        };
        Unsafe.Write(_frameConstantsMappedPtrs[frameIndex].ToPointer(), frameConstants);

        var instancePtr = (InstanceData*)_instanceMappedPtrs[frameIndex].ToPointer();
        for (var i = 0; i < NumObjects; i++)
        {
            ref var inst = ref _instances[i];
            var pos = new Vector3(inst.Model.M41, inst.Model.M42, inst.Model.M43);

            var rotSpeed = 0.5f + (i % 10) * 0.1f;
            var rotation = Matrix4x4.CreateRotationY(_time * rotSpeed) *
                           Matrix4x4.CreateRotationX(_time * rotSpeed * 0.7f);

            instancePtr[i] = new InstanceData
            {
                Model = rotation * Matrix4x4.CreateTranslation(pos),
                Color = inst.Color
            };
        }

        // Transition render target for rendering
        // Note: This uses the underlying command list - we'd need to expose a method for this
        // For now, we'll use PipelineBarrier
        var barrier = new PipelineBarrierDesc
        {
            TextureBarriers = TextureBarrierDescArray.Create([
                new TextureBarrierDesc
                {
                    Resource = renderTarget,
                    OldState = (uint)ResourceUsageFlagBits.Present,
                    NewState = (uint)ResourceUsageFlagBits.RenderTarget,
                    SourceQueue = QueueType.Graphics,
                    DestinationQueue = QueueType.Graphics
                }
            ])
        };
        _rgCommandList.PipelineBarrier(in barrier);
        _rtAttachments[0] = new RenderingAttachmentDesc
        {
            Resource = renderTarget,
            LoadOp = LoadOp.Clear,
            ClearColor = new Float4 { X = 0.1f, Y = 0.1f, Z = 0.15f, W = 1.0f }
        };

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachments.Handle, 1),
            NumLayers = 1
        };

        _rgCommandList.BeginRendering(renderingDesc);
        _rgCommandList.BindViewport(_viewport.X, _viewport.Y, _viewport.Width, _viewport.Height);
        _rgCommandList.BindScissorRect(_viewport.X, _viewport.Y, _viewport.Width, _viewport.Height);

        _rgCommandList.SetShader(_shader, "default");
        _rgCommandList.SetBuffer("FrameConstants", _frameConstantsBuffers[frameIndex]);
        _rgCommandList.SetBuffer("Instances", _instanceBuffers[frameIndex]);
        _rgCommandList.DrawMesh(_cubeMesh, NumObjects);

        var presentBarrier = new PipelineBarrierDesc
        {
            TextureBarriers = TextureBarrierDescArray.Create([
                new TextureBarrierDesc
                {
                    Resource = renderTarget,
                    OldState = (uint)ResourceUsageFlagBits.RenderTarget,
                    NewState = (uint)ResourceUsageFlagBits.Present,
                    SourceQueue = QueueType.Graphics,
                    DestinationQueue = QueueType.Graphics
                }
            ])
        };
        _rgCommandList.PipelineBarrier(in presentBarrier);
        _rgCommandList.Submit();
        
        _swapChain.Present(image);
    }

    public void Dispose()
    {
        _commandQueue.WaitIdle();
        for (var i = 0; i < NumFrames; i++)
        {
            _instanceBuffers[i].UnmapMemory();
            _frameConstantsBuffers[i].UnmapMemory();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex(Vector3 position, Vector3 normal, Vector2 texCoord)
    {
        public Vector3 Position = position;
        public Vector3 Normal = normal;
        public Vector2 TexCoord = texCoord;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InstanceData
    {
        public Matrix4x4 Model;
        public Vector4 Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FrameConstants
    {
        public Matrix4x4 ViewProjection;
        public Vector3 CameraPosition;
        public float Time;
    }
}

public static class RgCommandListDemoProgram
{
    public static void RunDemo()
    {
        Engine.Init(new EngineDesc());
        var preference = new APIPreference
        {
            Windows = APIPreferenceWindows.Vulkan
        };
        using var graphicsApi = new GraphicsApi(preference);
        using var logicalDevice = graphicsApi.CreateAndLoadOptimalLogicalDevice(new LogicalDeviceDesc()
        {
            EnableValidationLayers = true
        });
        
        {
            using var demo = new RgCommandListDemo(logicalDevice, 1920, 1080, "RgCommandList Demo - 1000 Cubes");
            while (demo.PollAndRender())
            {
            }
        }
        Engine.Shutdown();
    }
}