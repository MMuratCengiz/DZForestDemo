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
using DenOfIz;
using Graphics.ImGui;
using ImGuiNET;

namespace DZForestDemo;

public class ImGuiDemoWindow : IDisposable
{
    private readonly CommandQueue _commandQueue;
    private readonly SemaphoreArray _emptySemaphoreArray;
    private readonly FrameSync _frameSync;
    private readonly ImGuiRenderer _imGuiRenderer;
    private readonly ResourceTracking _resourceTracking;
    private readonly StepTimer _stepTimer = new();
    private readonly SwapChain _swapChain;
    private readonly Viewport _viewport;
    private readonly Window _window;
    private Vector3 _clearColor = new(0.45f, 0.55f, 0.60f);
    private int _counter;

    private bool _disposed;
    private float _floatValue;
    private bool _showAnotherWindow;
    private bool _showDemoWindow = true;

    public ImGuiDemoWindow(LogicalDevice logicalDevice, uint width, uint height, string title)
    {
        _window = new Window(new WindowDesc
        {
            Width = (int)width,
            Height = (int)height,
            Title = StringView.Create(title)
        });

        var commandQueueDesc = new CommandQueueDesc
        {
            QueueType = QueueType.Graphics
        };
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
            NumBuffers = 3
        };
        _swapChain = logicalDevice.CreateSwapChain(swapChainDesc);

        _frameSync = new FrameSync(new FrameSyncDesc
        {
            Device = logicalDevice,
            CommandQueue = _commandQueue,
            SwapChain = _swapChain,
            NumFrames = 3
        });

        _window.Show();

        _resourceTracking = new ResourceTracking();
        const uint numFrames = 3;
        for (uint i = 0; i < numFrames; ++i)
        {
            _resourceTracking.TrackTexture(_swapChain.GetRenderTarget(i), QueueType.Graphics);
        }

        _emptySemaphoreArray = new SemaphoreArray();
        _viewport = _swapChain.GetViewport();
        var imguiDesc = new ImGuiBackendDesc
        {
            LogicalDevice = logicalDevice,
            Viewport = _viewport,
            NumFrames = 3,
            MaxVertices = 65536,
            MaxIndices = 65536 * 3,
            MaxTextures = 128,
            RenderTargetFormat = Format.B8G8R8A8Unorm
        };
        _imGuiRenderer = new ImGuiRenderer(imguiDesc);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _frameSync.WaitIdle();
        _commandQueue.WaitIdle();

        _imGuiRenderer.Dispose();
        _frameSync.Dispose();
        _swapChain.Dispose();
        _commandQueue.Dispose();
        _resourceTracking.Dispose();
        _window.Dispose();

        GC.SuppressFinalize(this);
    }

    public bool PollAndRender()
    {
        while (InputSystem.PollEvent(out var ev))
        {
            _imGuiRenderer.ProcessEvent(ev);
            switch (ev.Type)
            {
                case EventType.Quit:
                    return false;
                case EventType.KeyDown when ev.Key.KeyCode == KeyCode.Escape:
                    if (!ImGui.GetIO().WantCaptureKeyboard)
                    {
                        return false;
                    }

                    break;
            }
        }

        _stepTimer.Tick();

        var frameIndex = _frameSync.NextFrame();
        var image = _frameSync.AcquireNextImage();

        var renderTarget = _swapChain.GetRenderTarget(image);
        var commandList = _frameSync.GetCommandList(frameIndex);

        _imGuiRenderer.NewFrame((uint)_viewport.Width, (uint)_viewport.Height, (float)_stepTimer.GetDeltaTime());
        BuildImGuiUi();

        Render(commandList, renderTarget, frameIndex);

        _frameSync.ExecuteCommandList(frameIndex, _emptySemaphoreArray);
        _frameSync.Present(image);

        return true;
    }

    private void BuildImGuiUi()
    {
        if (_showDemoWindow)
        {
            ImGui.ShowDemoWindow(ref _showDemoWindow);
        }

        {
            ImGui.Begin("Hello DenOfIz!");

            ImGui.Text("This is a simple ImGui window integrated with DenOfIz RHI.");
            ImGui.Checkbox("Demo Window", ref _showDemoWindow);
            ImGui.Checkbox("Another Window", ref _showAnotherWindow);

            ImGui.SliderFloat("float", ref _floatValue, 0.0f, 1.0f);
            ImGui.ColorEdit3("clear color", ref _clearColor);

            if (ImGui.Button("Button"))
            {
                _counter++;
            }

            ImGui.SameLine();
            ImGui.Text($"counter = {_counter}");

            var io = ImGui.GetIO();
            ImGui.Text($"DenOfIz.Application average {1000.0f / io.Framerate:0.000} ms/frame ({io.Framerate:0.0} FPS)");

            ImGui.Separator();
            ImGui.Text("Debug Info:");
            ImGui.Text($"Mouse pos: {io.MousePos.X:0.0}, {io.MousePos.Y:0.0}");
            ImGui.Text($"Mouse down: L={io.MouseDown[0]}, R={io.MouseDown[1]}, M={io.MouseDown[2]}");
            ImGui.Text($"WantCaptureMouse: {io.WantCaptureMouse}");
            ImGui.Text($"WantCaptureKeyboard: {io.WantCaptureKeyboard}");

            ImGui.End();
        }

        if (!_showAnotherWindow)
        {
            return;
        }

        ImGui.Begin("Another Window", ref _showAnotherWindow);
        ImGui.Text("Hello from another window!");
        if (ImGui.Button("Close Me"))
        {
            _showAnotherWindow = false;
        }

        ImGui.End();
    }

    private void Render(CommandList commandList, Texture renderTarget, uint frameIndex)
    {
        commandList.Begin();

        _resourceTracking.TransitionTexture(commandList, renderTarget,
            (uint)ResourceUsageFlagBits.RenderTarget, QueueType.Graphics);

        using var rtAttachments = new PinnedArray<RenderingAttachmentDesc>(1);
        rtAttachments[0] = new RenderingAttachmentDesc
        {
            Resource = renderTarget,
            LoadOp = LoadOp.Clear,
            ClearColor = new Float4
            {
                X = _clearColor.X,
                Y = _clearColor.Y,
                Z = _clearColor.Z,
                W = 1.0f
            }
        };

        var renderingDesc = new RenderingDesc
        {
            RTAttachments = RenderingAttachmentDescArray.FromPinned(rtAttachments.Handle, 1),
            NumLayers = 1
        };

        commandList.BeginRendering(renderingDesc);
        commandList.BindViewport(_viewport.X, _viewport.Y, _viewport.Width, _viewport.Height);
        commandList.BindScissorRect(_viewport.X, _viewport.Y, _viewport.Width, _viewport.Height);
        commandList.EndRendering();

        _imGuiRenderer.Render(renderTarget, commandList, frameIndex);

        _resourceTracking.TransitionTexture(commandList, renderTarget,
            (uint)ResourceUsageFlagBits.Present, QueueType.Graphics);

        commandList.End();
    }
}

public static class ImGuiDemoProgram
{
    public static void RunDemo()
    {
        Engine.Init(new EngineDesc());
        var preference = new APIPreference
        {
            Windows = APIPreferenceWindows.Directx12
        };
        using var graphicsApi = new GraphicsApi(preference);
        using var logicalDevice = graphicsApi.CreateAndLoadOptimalLogicalDevice(new LogicalDeviceDesc());

        using var demoWindow = new ImGuiDemoWindow(logicalDevice, 1920, 1080, "ImGui Demo - DenOfIz");
        while (demoWindow.PollAndRender())
        {
        }
    }
}