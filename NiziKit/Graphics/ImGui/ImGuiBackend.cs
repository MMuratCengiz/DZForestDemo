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
using System.Runtime.InteropServices;
using System.Text;
using DenOfIz;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using NiziKit.Core;
using Buffer = DenOfIz.Buffer;

namespace NiziKit.Graphics.ImGui;

public struct ImGuiBackendDesc
{
    public LogicalDevice LogicalDevice;
    public Viewport Viewport;
    public uint NumFrames;
    public uint MaxVertices;
    public uint MaxIndices;
    public uint MaxTextures;
    public Format RenderTargetFormat;

    public static ImGuiBackendDesc Default(LogicalDevice logicalDevice, Viewport viewport)
    {
        return new ImGuiBackendDesc
        {
            LogicalDevice = logicalDevice,
            Viewport = viewport,
            NumFrames = 3,
            MaxVertices = 65536,
            MaxIndices = 65536 * 3,
            MaxTextures = 128,
            RenderTargetFormat = Format.B8G8R8A8Unorm
        };
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct ImGuiUniforms
{
    public Matrix4x4 Projection;
    public Vector4 ScreenSize;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PixelConstants
{
    public uint TextureIndex;
    private readonly uint _pad0;
}

internal class ImGuiFrameData
{
    public CommandList? CommandList;
    public BindGroup? ConstantsBindGroup;
    public Fence? FrameFence;
    public BindGroup? TextureBindGroup;
}

public class ImGuiBackend : IDisposable
{
    private static readonly ILogger Logger = Log.Get<ImGuiBackend>();
    private const string ImGuiVertexShaderSource = @"
struct VSInput
{
    float2 Position : POSITION;
    float2 TexCoord : TEXCOORD0;
    uint   Color : COLOR0;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
};

cbuffer ImGuiUniforms : register(b0, space1)
{
    float4x4 Projection;
    float4 ScreenSize;
};

VSOutput main(VSInput input)
{
    VSOutput output;
    output.Position = mul(float4(input.Position, 0.0, 1.0), Projection);
    output.TexCoord = input.TexCoord;
    output.Color = float4(
        ((input.Color >> 0) & 0xFF) / 255.0f,
        ((input.Color >> 8) & 0xFF) / 255.0f,
        ((input.Color >> 16) & 0xFF) / 255.0f,
        ((input.Color >> 24) & 0xFF) / 255.0f
    );
    return output;
}";

    private const string ImGuiPixelShaderSource = @"
struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
};

cbuffer PixelConstants : register(b0, space2)
{
    uint TextureIndex;
    uint _Pad0;
};

Texture2D Textures[128] : register(t0, space0);
SamplerState LinearSampler : register(s0, space0);

float4 main(PSInput input) : SV_TARGET
{
    float4 texColor = Textures[TextureIndex].Sample(LinearSampler, input.TexCoord);
    return texColor * input.Color;
}";

    private readonly ImGuiBackendDesc _desc;

    private readonly ImGuiFrameData[] _frameData;
    private readonly LogicalDevice _logicalDevice;
    private readonly BindGroup?[] _pixelConstantsBindGroups;

    private readonly Texture?[] _textures;
    private uint _alignedPixelConstantsSize;
    private uint _alignedUniformSize;
    private BindGroupLayout[]? _bindGroupLayouts;
    private CommandListPool? _commandListPool;

    private CommandQueue? _commandQueue;
    private Texture? _fontTexture;
    private Buffer? _indexBuffer;
    private IntPtr _indexBufferData;
    private InputLayout? _inputLayout;
    private uint _nextFrame;
    private Texture? _nullTexture;
    private Pipeline? _pipeline;
    private Buffer? _pixelConstantsBuffer;
    private IntPtr _pixelConstantsData;

    private Matrix4x4 _projectionMatrix;
    private RootSignature? _rootSignature;
    private Sampler? _sampler;

    private ShaderProgram? _shaderProgram;
    private bool _textInputActive;
    private bool _texturesDirty = true;
    private Buffer? _uniformBuffer;
    private IntPtr _uniformBufferData;

    private Buffer? _vertexBuffer;
    private IntPtr _vertexBufferData;
    private Viewport _viewport;

    public ImGuiBackend(ImGuiBackendDesc desc)
    {
        _desc = desc;
        _logicalDevice = desc.LogicalDevice;
        _viewport = desc.Viewport;

        _textures = new Texture?[desc.MaxTextures];
        _frameData = new ImGuiFrameData[desc.NumFrames];
        _pixelConstantsBindGroups = new BindGroup?[desc.MaxTextures];

        for (var i = 0; i < desc.NumFrames; i++)
        {
            _frameData[i] = new ImGuiFrameData();
        }

        CreateCommandInfrastructure();
        CreateShaderProgram();
        CreatePipeline();
        CreateNullTexture();
        CreateBuffers();
        CreateFontTexture();
        SetViewport(_viewport);
        CreateSampler();
        UpdateTextureBindings();
    }

    public Viewport Viewport => _viewport;

    public void Dispose()
    {
        if (_textInputActive)
        {
            InputSystem.StopTextInput();
        }

        _vertexBuffer?.UnmapMemory();
        _indexBuffer?.UnmapMemory();
        _uniformBuffer?.UnmapMemory();
        _pixelConstantsBuffer?.UnmapMemory();

        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _uniformBuffer?.Dispose();
        _pixelConstantsBuffer?.Dispose();

        foreach (var frameData in _frameData)
        {
            frameData.ConstantsBindGroup?.Dispose();
            frameData.TextureBindGroup?.Dispose();
            frameData.FrameFence?.Dispose();
        }

        foreach (var bindGroup in _pixelConstantsBindGroups)
        {
            bindGroup?.Dispose();
        }

        _fontTexture?.Dispose();
        _nullTexture?.Dispose();

        _sampler?.Dispose();
        _pipeline?.Dispose();
        _rootSignature?.Dispose();
        if (_bindGroupLayouts != null)
        {
            foreach (var layout in _bindGroupLayouts)
            {
                layout?.Dispose();
            }
        }
        _inputLayout?.Dispose();
        _shaderProgram?.Dispose();
        _commandListPool?.Dispose();
        _commandQueue?.Dispose();

        GC.SuppressFinalize(this);
    }

    public void SetViewport(Viewport viewport)
    {
        _viewport = viewport;

        const float zn = 0.0f;
        const float zf = 1.0f;

        _projectionMatrix = new Matrix4x4
        {
            M11 = 2.0f / (viewport.Width - viewport.X),
            M12 = 0,
            M13 = 0,
            M14 = 0,
            M21 = 0,
            M22 = 2.0f / (viewport.Y - viewport.Height),
            M23 = 0,
            M24 = 0,
            M31 = 0,
            M32 = 0,
            M33 = 1.0f / (zf - zn),
            M34 = 0,
            M41 = (viewport.X + viewport.Width) / (viewport.X - viewport.Width),
            M42 = (viewport.Height + viewport.Y) / (viewport.Height - viewport.Y),
            M43 = zn / (zn - zf),
            M44 = 1.0f
        };
    }

    private void CreateCommandInfrastructure()
    {
        var commandQueueDesc = new CommandQueueDesc
        {
            QueueType = QueueType.Graphics
        };
        _commandQueue = _logicalDevice.CreateCommandQueue(commandQueueDesc);

        var poolDesc = new CommandListPoolDesc
        {
            CommandQueue = _commandQueue,
            NumCommandLists = _desc.NumFrames
        };
        _commandListPool = _logicalDevice.CreateCommandListPool(poolDesc);

        var commandLists = _commandListPool.GetCommandLists();
        var commandListArray = commandLists.ToArray();
        for (var i = 0; i < _desc.NumFrames && i < commandListArray.Length; i++)
        {
            _frameData[i].CommandList = commandListArray[i];
            _frameData[i].FrameFence = _logicalDevice.CreateFence();
        }
    }

    private void CreateShaderProgram()
    {
        using var vsData = ByteArray.Create(Encoding.UTF8.GetBytes(ImGuiVertexShaderSource));
        using var psData = ByteArray.Create(Encoding.UTF8.GetBytes(ImGuiPixelShaderSource));

        var vsDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Vertex,
            Data = vsData,
            EntryPoint = StringView.Create("main")
        };

        var psDesc = new ShaderStageDesc
        {
            Stage = (uint)ShaderStageFlagBits.Pixel,
            Data = psData,
            EntryPoint = StringView.Create("main")
        };

        using var shaderStagesArray = ShaderStageDescArray.Create([vsDesc, psDesc]);

        var programDesc = new ShaderProgramDesc
        {
            ShaderStages = shaderStagesArray
        };

        _shaderProgram = new ShaderProgram(programDesc);
    }

    private void CreatePipeline()
    {
        var reflection = _shaderProgram!.Reflect();

        // Create BindGroupLayouts from reflection
        var bindGroupLayoutDescs = reflection.BindGroupLayouts.ToArray();
        _bindGroupLayouts = new BindGroupLayout[bindGroupLayoutDescs.Length];
        for (var i = 0; i < bindGroupLayoutDescs.Length; i++)
        {
            _bindGroupLayouts[i] = _logicalDevice.CreateBindGroupLayout(bindGroupLayoutDescs[i]);
        }

        // Create RootSignature from BindGroupLayouts
        var rootSigDesc = new RootSignatureDesc
        {
            BindGroupLayouts = BindGroupLayoutArray.Create(_bindGroupLayouts),
            RootConstants = reflection.RootConstants
        };
        _rootSignature = _logicalDevice.CreateRootSignature(rootSigDesc);
        _inputLayout = _logicalDevice.CreateInputLayout(reflection.InputLayout);

        var blendDesc = new BlendDesc
        {
            Enable = true,
            SrcBlend = Blend.SrcAlpha,
            DstBlend = Blend.InvSrcAlpha,
            BlendOp = BlendOp.Add,
            SrcBlendAlpha = Blend.One,
            DstBlendAlpha = Blend.InvSrcAlpha,
            BlendOpAlpha = BlendOp.Add,
            RenderTargetWriteMask = 0x0F
        };

        var renderTarget = new RenderTargetDesc
        {
            Format = _desc.RenderTargetFormat,
            Blend = blendDesc
        };

        using var renderTargets = RenderTargetDescArray.Create([renderTarget]);

        var pipelineDesc = new PipelineDesc
        {
            RootSignature = _rootSignature,
            InputLayout = _inputLayout,
            ShaderProgram = _shaderProgram,
            BindPoint = BindPoint.Graphics,
            Graphics = new GraphicsPipelineDesc
            {
                PrimitiveTopology = PrimitiveTopology.Triangle,
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                DepthTest = new DepthTest
                {
                    Enable = false,
                    CompareOp = CompareOp.Always,
                    Write = false
                },
                RenderTargets = renderTargets
            }
        };

        _pipeline = _logicalDevice.CreatePipeline(pipelineDesc);
    }

    private void CreateBuffers()
    {
        var vertexBufferDesc = new BufferDesc
        {
            NumBytes = _desc.MaxVertices * 20,
            Usage = (uint)BufferUsageFlagBits.Vertex,
            HeapType = HeapType.CpuGpu,
            DebugName = StringView.Intern("ImGui Vertex Buffer")
        };
        _vertexBuffer = _logicalDevice.CreateBuffer(vertexBufferDesc);
        _vertexBufferData = _vertexBuffer.MapMemory();

        var indexBufferDesc = new BufferDesc
        {
            NumBytes = _desc.MaxIndices * 2,
            Usage = (uint)BufferUsageFlagBits.Index,
            HeapType = HeapType.CpuGpu,
            DebugName = StringView.Intern("ImGui Index Buffer")
        };
        _indexBuffer = _logicalDevice.CreateBuffer(indexBufferDesc);
        _indexBufferData = _indexBuffer.MapMemory();

        _alignedUniformSize = (uint)((Marshal.SizeOf<ImGuiUniforms>() + 255) & ~255);
        var uniformBufferDesc = new BufferDesc
        {
            NumBytes = _alignedUniformSize * _desc.NumFrames,
            Usage = (uint)BufferUsageFlagBits.Uniform,
            HeapType = HeapType.CpuGpu,
            DebugName = StringView.Intern("ImGui Uniform Buffer")
        };
        _uniformBuffer = _logicalDevice.CreateBuffer(uniformBufferDesc);
        _uniformBufferData = _uniformBuffer.MapMemory();

        _alignedPixelConstantsSize = (uint)((Marshal.SizeOf<PixelConstants>() + 255) & ~255);
        var pixelConstantsBufferDesc = new BufferDesc
        {
            NumBytes = _alignedPixelConstantsSize * _desc.MaxTextures,
            Usage = (uint)BufferUsageFlagBits.Uniform,
            HeapType = HeapType.CpuGpu,
            DebugName = StringView.Intern("ImGui Pixel Constants Buffer")
        };
        _pixelConstantsBuffer = _logicalDevice.CreateBuffer(pixelConstantsBufferDesc);
        _pixelConstantsData = _pixelConstantsBuffer.MapMemory();

        InitializePixelConstants();

        for (uint frameIdx = 0; frameIdx < _desc.NumFrames; frameIdx++)
        {
            // space1 for constants (uniforms)
            var constantsBindGroupDesc = new BindGroupDesc
            {
                Layout = _bindGroupLayouts![1]
            };

            _frameData[frameIdx].ConstantsBindGroup = _logicalDevice.CreateBindGroup(constantsBindGroupDesc);
            _frameData[frameIdx].ConstantsBindGroup!.BeginUpdate();
            _frameData[frameIdx].ConstantsBindGroup!.CbvWithDesc(new BindBufferDesc
            {
                Resource = _uniformBuffer,
                ResourceOffset = frameIdx * _alignedUniformSize
            });
            _frameData[frameIdx].ConstantsBindGroup!.EndUpdate();

            // space0 for textures/sampler
            var textureBindGroupDesc = new BindGroupDesc
            {
                Layout = _bindGroupLayouts[0]
            };
            _frameData[frameIdx].TextureBindGroup = _logicalDevice.CreateBindGroup(textureBindGroupDesc);
        }

        for (uint texIdx = 0; texIdx < _desc.MaxTextures; texIdx++)
        {
            // space2 for pixel constants
            var pixelConstantsBindGroupDesc = new BindGroupDesc
            {
                Layout = _bindGroupLayouts![2]
            };
            _pixelConstantsBindGroups[texIdx] = _logicalDevice.CreateBindGroup(pixelConstantsBindGroupDesc);

            _pixelConstantsBindGroups[texIdx]!.BeginUpdate();
            _pixelConstantsBindGroups[texIdx]!.CbvWithDesc(new BindBufferDesc
            {
                Resource = _pixelConstantsBuffer,
                ResourceOffset = texIdx * _alignedPixelConstantsSize
            });
            _pixelConstantsBindGroups[texIdx]!.EndUpdate();
        }
    }

    private unsafe void InitializePixelConstants()
    {
        for (uint i = 0; i < _desc.MaxTextures; i++)
        {
            var ptr = (PixelConstants*)((byte*)_pixelConstantsData + i * _alignedPixelConstantsSize);
            ptr->TextureIndex = i;
        }
    }

    private static unsafe void CopyFontData(IntPtr src, IntPtr dst, int size)
    {
        System.Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), size, size);
    }

    private void CreateNullTexture()
    {
        var textureDesc = new TextureDesc
        {
            Width = 1,
            Height = 1,
            Depth = 1,
            ArraySize = 1,
            MipLevels = 1,
            Format = Format.R8G8B8A8Unorm,
            Usage = (uint)TextureUsageFlagBits.TextureBinding,
            HeapType = HeapType.Gpu,
            DebugName = StringView.Intern("ImGui Null Texture")
        };

        _nullTexture = _logicalDevice.CreateTexture(textureDesc);
        _textures[1] = _nullTexture;
    }

    private void CreateFontTexture()
    {
        var io = ImGuiNET.ImGui.GetIO();

        io.Fonts.GetTexDataAsRGBA32(out IntPtr fontData, out var fontWidth, out var fontHeight, out var bytesPerPixel);
        var fontDataSize = fontWidth * fontHeight * bytesPerPixel;

        var fontTextureDesc = new TextureDesc
        {
            Width = (uint)fontWidth,
            Height = (uint)fontHeight,
            Depth = 1,
            ArraySize = 1,
            MipLevels = 1,
            Format = Format.R8G8B8A8Unorm,
            Usage = (uint)(TextureUsageFlagBits.TextureBinding | TextureUsageFlagBits.CopyDst),
            HeapType = HeapType.Gpu,
            DebugName = StringView.Intern("ImGui Font Texture")
        };
        _fontTexture = _logicalDevice.CreateTexture(fontTextureDesc);
        var uploadBufferDesc = new BufferDesc
        {
            NumBytes = (ulong)fontDataSize,
            Usage = (uint)BufferUsageFlagBits.CopySrc,
            HeapType = HeapType.CpuGpu,
            DebugName = StringView.Intern("ImGui Font Upload Buffer")
        };
        var uploadBuffer = _logicalDevice.CreateBuffer(uploadBufferDesc);

        var uploadData = uploadBuffer.MapMemory();
        CopyFontData(fontData, uploadData, fontDataSize);
        uploadBuffer.UnmapMemory();
        using var resourceTracking = new ResourceTracking();
        resourceTracking.TrackTexture(_fontTexture, QueueType.Graphics);
        resourceTracking.TrackBuffer(uploadBuffer, QueueType.Graphics);

        var commandLists = _commandListPool!.GetCommandLists();
        var commandListArray = commandLists.ToArray();
        var commandList = commandListArray[0]!;

        commandList.Begin();

        var copyDesc = new CopyBufferToTextureDesc
        {
            DstTexture = _fontTexture,
            SrcBuffer = uploadBuffer,
            SrcOffset = 0,
            DstX = 0,
            DstY = 0,
            DstZ = 0,
            Format = Format.R8G8B8A8Unorm,
            MipLevel = 0,
            ArrayLayer = 0,
            RowPitch = (uint)(fontWidth * 4),
            NumRows = (uint)fontHeight
        };
        commandList.CopyBufferToTexture(copyDesc);

        resourceTracking.TransitionTexture(commandList, _fontTexture, (uint)ResourceUsageFlagBits.ShaderResource,
            QueueType.Graphics);

        commandList.End();

        using var uploadFence = _logicalDevice.CreateFence();

        using var pinnedCommandLists = CommandListArray.Create([commandList]);
        var executeDesc = new ExecuteCommandListsDesc
        {
            Signal = uploadFence,
            CommandLists = pinnedCommandLists.Value
        };
        _commandQueue!.ExecuteCommandLists(executeDesc);
        uploadFence.Wait();

        uploadBuffer.Dispose();

        _textures[0] = _fontTexture;
        io.Fonts.SetTexID(IntPtr.Zero);
        _texturesDirty = true;
    }

    private void CreateSampler()
    {
        var samplerDesc = new SamplerDesc
        {
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MipmapMode = MipmapMode.Nearest
        };
        _sampler = _logicalDevice.CreateSampler(samplerDesc);
    }

    private void UpdateTextureBindings()
    {
        if (!_texturesDirty)
        {
            return;
        }

        var textureHandles = new ulong[_desc.MaxTextures];
        for (uint i = 0; i < _desc.MaxTextures; i++)
        {
            var tex = _textures[i] ?? _nullTexture;
            textureHandles[i] = tex!;
        }

        using var textureArrayPinned = new PinnedArray<ulong>(textureHandles);
        var textureArray = new TextureArray
        {
            Elements = textureArrayPinned.Pointer,
            NumElements = _desc.MaxTextures
        };

        for (uint frameIndex = 0; frameIndex < _desc.NumFrames; frameIndex++)
        {
            _frameData[frameIndex].TextureBindGroup!.BeginUpdate();
            _frameData[frameIndex].TextureBindGroup!.SrvArray(0, textureArray);
            _frameData[frameIndex].TextureBindGroup!.Sampler(0, _sampler);
            _frameData[frameIndex].TextureBindGroup!.EndUpdate();
        }

        _texturesDirty = false;
    }

    public IntPtr AddTexture(Texture texture)
    {
        for (uint i = 2; i < _desc.MaxTextures; i++)
        {
            if (_textures[i] != null)
            {
                continue;
            }

            _textures[i] = texture;
            _texturesDirty = true;
            return (IntPtr)i;
        }

        throw new InvalidOperationException("ImGui texture capacity exceeded");
    }

    public void RemoveTexture(IntPtr textureId)
    {
        var index = (uint)textureId;
        if (index < 2 || index >= _desc.MaxTextures)
        {
            return;
        }

        _textures[index] = null;
        _texturesDirty = true;
    }

    public void RecreateFonts()
    {
        CreateFontTexture();
    }

    public void RenderDrawData(CommandList commandList, ImDrawDataPtr drawData, uint frameIndex)
    {
        if (!drawData.Valid || drawData.CmdListsCount == 0)
        {
            return;
        }

        _nextFrame = (_nextFrame + 1) % _desc.NumFrames;

        UpdateTextureBindings();

        ulong totalVertexSize = 0;
        ulong totalIndexSize = 0;
        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            totalVertexSize += (ulong)(cmdList.VtxBuffer.Size * 20);
            totalIndexSize += (ulong)(cmdList.IdxBuffer.Size * 2);
        }

        if (totalVertexSize > _desc.MaxVertices * 20 || totalIndexSize > _desc.MaxIndices * 2)
        {
            Logger.LogWarning("ImGui draw data exceeds buffer capacity");
            return;
        }

        uint vertexOffset = 0;
        uint indexOffset = 0;

        unsafe
        {
            for (var n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdLists[n];

                var vertexDataSize = cmdList.VtxBuffer.Size * 20;
                var indexDataSize = cmdList.IdxBuffer.Size * 2;

                System.Buffer.MemoryCopy(
                    cmdList.VtxBuffer.Data.ToPointer(),
                    (byte*)_vertexBufferData + vertexOffset * 20,
                    vertexDataSize,
                    vertexDataSize);

                System.Buffer.MemoryCopy(
                    cmdList.IdxBuffer.Data.ToPointer(),
                    (byte*)_indexBufferData + indexOffset * 2,
                    indexDataSize,
                    indexDataSize);

                vertexOffset += (uint)cmdList.VtxBuffer.Size;
                indexOffset += (uint)cmdList.IdxBuffer.Size;
            }
        }

        SetupRenderState(commandList, frameIndex);

        vertexOffset = 0;
        indexOffset = 0;
        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            RenderImDrawList(commandList, cmdList, vertexOffset, indexOffset);
            vertexOffset += (uint)cmdList.VtxBuffer.Size;
            indexOffset += (uint)cmdList.IdxBuffer.Size;
        }
    }

    private void SetupRenderState(CommandList commandList, uint frameIndex)
    {
        commandList.BindPipeline(_pipeline);
        commandList.BindVertexBuffer(_vertexBuffer, 0, 20, 0);
        commandList.BindIndexBuffer(_indexBuffer, IndexType.Uint16, 0);
        unsafe
        {
            var uniformPtr = (ImGuiUniforms*)((byte*)_uniformBufferData + frameIndex * _alignedUniformSize);
            uniformPtr->Projection = _projectionMatrix;
            uniformPtr->ScreenSize = new Vector4
            {
                X = _viewport.Width,
                Y = _viewport.Height,
                Z = 0,
                W = 0
            };
        }

        commandList.BindGroup(_frameData[frameIndex].ConstantsBindGroup);
        commandList.BindGroup(_frameData[frameIndex].TextureBindGroup);
    }

    private void RenderImDrawList(CommandList commandList, ImDrawListPtr cmdList, uint vertexOffset, uint indexOffset)
    {
        for (var cmdIdx = 0; cmdIdx < cmdList.CmdBuffer.Size; cmdIdx++)
        {
            var pcmd = cmdList.CmdBuffer[cmdIdx];

            if (pcmd.UserCallback != IntPtr.Zero)
            {
                continue;
            }

            var clipMin = new Vector2(pcmd.ClipRect.X, pcmd.ClipRect.Y);
            var clipMax = new Vector2(pcmd.ClipRect.Z, pcmd.ClipRect.W);

            if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y)
            {
                continue;
            }

            commandList.BindScissorRect(clipMin.X, clipMin.Y, clipMax.X - clipMin.X, clipMax.Y - clipMin.Y);

            var textureIndex = (uint)pcmd.TextureId;
            if (textureIndex < _desc.MaxTextures && _textures[textureIndex] != null &&
                _pixelConstantsBindGroups[textureIndex] != null)
            {
                commandList.BindGroup(_pixelConstantsBindGroups[textureIndex]);
                commandList.DrawIndexed(
                    pcmd.ElemCount,
                    1,
                    pcmd.IdxOffset + indexOffset,
                    pcmd.VtxOffset + vertexOffset,
                    0);
            }
        }
    }

    public void ProcessEvent(Event ev)
    {
        var io = ImGuiNET.ImGui.GetIO();

        switch (ev.Type)
        {
            case EventType.MouseMotion:
                io.AddMousePosEvent(ev.MouseMotion.X, ev.MouseMotion.Y);
                break;

            case EventType.MouseButtonDown:
            case EventType.MouseButtonUp:
                {
                    io.AddMousePosEvent(ev.MouseButton.X, ev.MouseButton.Y);

                    var mouseButton = ev.MouseButton.Button switch
                    {
                        MouseButton.Left => 0,
                        MouseButton.Right => 1,
                        MouseButton.Middle => 2,
                        _ => 0
                    };
                    io.AddMouseButtonEvent(mouseButton, ev.Type == EventType.MouseButtonDown);
                }
                break;

            case EventType.MouseWheel:
                io.AddMouseWheelEvent(ev.MouseWheel.X, ev.MouseWheel.Y);
                break;

            case EventType.KeyDown:
            case EventType.KeyUp:
                {
                    var key = KeyCodeToImGuiKey(ev.Key.KeyCode);
                    if (key != ImGuiKey.None)
                    {
                        io.AddKeyEvent(key, ev.Type == EventType.KeyDown);
                    }
                }
                break;

            case EventType.TextInput:
                io.AddInputCharactersUTF8(ev.Text.Text);
                break;
        }
    }

    public void UpdateTextInputState()
    {
        var io = ImGuiNET.ImGui.GetIO();
        switch (io.WantTextInput)
        {
            case true when !_textInputActive:
                InputSystem.StartTextInput();
                _textInputActive = true;
                break;
            case false when _textInputActive:
                InputSystem.StopTextInput();
                _textInputActive = false;
                break;
        }
    }

    private static ImGuiKey KeyCodeToImGuiKey(KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.Tab => ImGuiKey.Tab,
            KeyCode.Left => ImGuiKey.LeftArrow,
            KeyCode.Right => ImGuiKey.RightArrow,
            KeyCode.Up => ImGuiKey.UpArrow,
            KeyCode.Down => ImGuiKey.DownArrow,
            KeyCode.Pageup => ImGuiKey.PageUp,
            KeyCode.Pagedown => ImGuiKey.PageDown,
            KeyCode.Home => ImGuiKey.Home,
            KeyCode.End => ImGuiKey.End,
            KeyCode.Insert => ImGuiKey.Insert,
            KeyCode.Delete => ImGuiKey.Delete,
            KeyCode.Backspace => ImGuiKey.Backspace,
            KeyCode.Space => ImGuiKey.Space,
            KeyCode.Return => ImGuiKey.Enter,
            KeyCode.Escape => ImGuiKey.Escape,
            KeyCode.Lctrl => ImGuiKey.LeftCtrl,
            KeyCode.Lshift => ImGuiKey.LeftShift,
            KeyCode.Lalt => ImGuiKey.LeftAlt,
            KeyCode.Lgui => ImGuiKey.LeftSuper,
            KeyCode.Rctrl => ImGuiKey.RightCtrl,
            KeyCode.Rshift => ImGuiKey.RightShift,
            KeyCode.Ralt => ImGuiKey.RightAlt,
            KeyCode.Rgui => ImGuiKey.RightSuper,
            KeyCode.A => ImGuiKey.A,
            KeyCode.C => ImGuiKey.C,
            KeyCode.V => ImGuiKey.V,
            KeyCode.X => ImGuiKey.X,
            KeyCode.Y => ImGuiKey.Y,
            KeyCode.Z => ImGuiKey.Z,
            _ => ImGuiKey.None
        };
    }
}

public class ImGuiRenderer : IDisposable
{
    private readonly ImGuiBackend _backend;

    private readonly PinnedArray<RenderingAttachmentDesc> _rtAttachments = new(1);

    public ImGuiRenderer(ImGuiBackendDesc desc)
    {
        ImGuiNET.ImGui.CreateContext();
        var io = ImGuiNET.ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

        ImGuiNET.ImGui.StyleColorsDark();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        _backend = new ImGuiBackend(desc);
    }

    public void Dispose()
    {
        _backend.Dispose();
        _rtAttachments.Dispose();
        ImGuiNET.ImGui.DestroyContext();

        GC.SuppressFinalize(this);
    }

    public void ProcessEvent(Event ev)
    {
        _backend.ProcessEvent(ev);
    }

    public void NewFrame(uint width, uint height, float deltaTime)
    {
        var io = ImGuiNET.ImGui.GetIO();
        io.DisplaySize = new Vector2(width, height);
        io.DeltaTime = deltaTime;
        ImGuiNET.ImGui.NewFrame();
        _backend.UpdateTextInputState();
    }

    public void Render(Texture renderTarget, CommandList commandList, uint frameIndex)
    {
        ImGuiNET.ImGui.Render();

        _rtAttachments[0] = new RenderingAttachmentDesc
        {
            Resource = renderTarget,
            LoadOp = LoadOp.Load
        };

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(_rtAttachments.Handle, 1),
            NumLayers = 1
        };

        commandList.BeginRendering(renderingDesc);

        var viewport = _backend.Viewport;
        commandList.BindViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height);
        commandList.BindScissorRect(viewport.X, viewport.Y, viewport.Width, viewport.Height);

        _backend.RenderDrawData(commandList, ImGuiNET.ImGui.GetDrawData(), frameIndex);

        commandList.EndRendering();
    }

    public void SetViewport(Viewport viewport)
    {
        _backend.SetViewport(viewport);
    }

    public void RecreateFonts()
    {
        _backend.RecreateFonts();
    }

    public IntPtr AddTexture(Texture texture)
    {
        return _backend.AddTexture(texture);
    }

    public void RemoveTexture(IntPtr textureId)
    {
        _backend.RemoveTexture(textureId);
    }
}
