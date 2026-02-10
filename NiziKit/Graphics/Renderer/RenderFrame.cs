using DenOfIz;
using NiziKit.Graphics.Resources;
using Semaphore = DenOfIz.Semaphore;
using RenderPass = NiziKit.Graphics.Renderer.Pass.RenderPass;
using GraphicsPass = NiziKit.Graphics.Renderer.Pass.GraphicsPass;
using ComputePass = NiziKit.Graphics.Renderer.Pass.ComputePass;
using RaytracingPass = NiziKit.Graphics.Renderer.Pass.RaytracingPass;

namespace NiziKit.Graphics.Renderer;

public partial class RenderFrame : IDisposable
{
    private const int MaxPassesPerFrame = 8;
    private const int MaxDependenciesPerPass = 8;

    private readonly Fence[] _frameFences;
    private readonly CommandListAllocator _commandListAllocator;

    private readonly GraphicsPass[][] _graphicsPasses;
    private readonly Semaphore[][] _graphicsSemaphores;
    private readonly ComputePass[][] _computePasses;
    private readonly Semaphore[][] _computeSemaphores;
    private readonly RaytracingPass[][] _raytracingPasses;
    private readonly Semaphore[][] _raytracingSemaphores;

    private readonly Pass.PresentPass[] _presentPasses;
    private readonly Semaphore[] _presentSemaphores;
    private readonly CommandList[] _presentCommandLists;

    private readonly PassData[] _passes;
    private int _passCount;
    private int _graphicsPassCount;
    private int _computePassCount;
    private int _raytracingPassCount;

    private readonly CommandList[] _submitCommandLists;
    private readonly Semaphore[] _submitSignalSemaphores;
    private readonly Semaphore[] _submitWaitSemaphores;

    private int _currentFrame;

    private struct PassData
    {
        public RenderPass Pass;
        public Semaphore SignalSemaphore;
        public int DependencyCount;
        public int Dep0, Dep1, Dep2, Dep3, Dep4, Dep5, Dep6, Dep7;

        public int GetDependency(int index)
        {
            return index switch
            {
                0 => Dep0,
                1 => Dep1,
                2 => Dep2,
                3 => Dep3,
                4 => Dep4,
                5 => Dep5,
                6 => Dep6,
                7 => Dep7,
                _ => -1
            };
        }

        public void SetDependency(int index, int value)
        {
            switch (index)
            {
                case 0: Dep0 = value; break;
                case 1: Dep1 = value; break;
                case 2: Dep2 = value; break;
                case 3: Dep3 = value; break;
                case 4: Dep4 = value; break;
                case 5: Dep5 = value; break;
                case 6: Dep6 = value; break;
                case 7: Dep7 = value; break;
            }
        }
    }

    public RenderFrame()
    {
        var numFrames = (int)GraphicsContext.NumFrames;

        _frameFences = new Fence[numFrames];
        for (var i = 0; i < numFrames; i++)
        {
            _frameFences[i] = GraphicsContext.Device.CreateFence();
        }

        _commandListAllocator = new CommandListAllocator();
        _passes = new PassData[MaxPassesPerFrame];

        _graphicsPasses = new GraphicsPass[numFrames][];
        _graphicsSemaphores = new Semaphore[numFrames][];
        _computePasses = new ComputePass[numFrames][];
        _computeSemaphores = new Semaphore[numFrames][];
        _raytracingPasses = new RaytracingPass[numFrames][];
        _raytracingSemaphores = new Semaphore[numFrames][];
        _presentPasses = new Pass.PresentPass[numFrames];
        _presentSemaphores = new Semaphore[numFrames];
        _presentCommandLists = new CommandList[numFrames];

        for (var frame = 0; frame < numFrames; frame++)
        {
            _graphicsPasses[frame] = new GraphicsPass[MaxPassesPerFrame];
            _graphicsSemaphores[frame] = new Semaphore[MaxPassesPerFrame];
            _computePasses[frame] = new ComputePass[MaxPassesPerFrame];
            _computeSemaphores[frame] = new Semaphore[MaxPassesPerFrame];
            _raytracingPasses[frame] = new RaytracingPass[MaxPassesPerFrame];
            _raytracingSemaphores[frame] = new Semaphore[MaxPassesPerFrame];

            for (var i = 0; i < MaxPassesPerFrame; i++)
            {
                var (gfxCmd, gfxSem) = _commandListAllocator.GetCommandList(QueueType.Graphics, (uint)frame);
                _graphicsPasses[frame][i] = new GraphicsPass(gfxCmd);
                _graphicsSemaphores[frame][i] = gfxSem;

                var (compCmd, compSem) = _commandListAllocator.GetCommandList(QueueType.Compute, (uint)frame);
                _computePasses[frame][i] = new ComputePass(compCmd);
                _computeSemaphores[frame][i] = compSem;

                var (rtCmd, rtSem) = _commandListAllocator.GetCommandList(QueueType.Graphics, (uint)frame);
                _raytracingPasses[frame][i] = new RaytracingPass(rtCmd);
                _raytracingSemaphores[frame][i] = rtSem;
            }

            var (presentCmd, presentSem) = _commandListAllocator.GetCommandList(QueueType.Graphics, (uint)frame);
            _presentPasses[frame] = new Pass.PresentPass();
            _presentSemaphores[frame] = presentSem;
            _presentCommandLists[frame] = presentCmd;
        }

        _submitCommandLists = new CommandList[1];
        _submitSignalSemaphores = new Semaphore[1];
        _submitWaitSemaphores = new Semaphore[MaxDependenciesPerPass];
    }

    public void BeginFrame()
    {
        if (Metal.IsCapturing(GraphicsContext.Device))
        {
            Metal.EndGpuCapture(GraphicsContext.Device);
        }

        if (InputSystem.GetKeyState(KeyCode.F12) == KeyState.Pressed &&
            InputSystem.GetKeyState(KeyCode.Lgui) == KeyState.Pressed)
        {
            Metal.BeginGpuCapture(GraphicsContext.Device);
        }

        GraphicsContext.BeginFrame();
        _currentFrame = GraphicsContext.FrameIndex;
        _frameFences[_currentFrame].Wait();
        _passCount = 0;
        _graphicsPassCount = 0;
        _computePassCount = 0;
        _raytracingPassCount = 0;
        ResetBlitPassIndex();
        ResetUi();
    }

    public GraphicsPass BeginGraphicsPass()
    {
        return BeginGraphicsPassInternal(-1);
    }

    public GraphicsPass BeginGraphicsPass(RenderPass dependency)
    {
        var depIndex = FindPassIndex(dependency);
        return BeginGraphicsPassInternal(depIndex);
    }

    public GraphicsPass BeginGraphicsPass(RenderPass dependency1, RenderPass dependency2)
    {
        var pass = BeginGraphicsPassInternal(FindPassIndex(dependency1));
        AddDependency(_passCount - 1, FindPassIndex(dependency2));
        return pass;
    }

    public ComputePass BeginComputePass()
    {
        return BeginComputePassInternal(-1);
    }

    public ComputePass BeginComputePass(RenderPass dependency)
    {
        var depIndex = FindPassIndex(dependency);
        return BeginComputePassInternal(depIndex);
    }

    public RaytracingPass BeginRaytracingPass()
    {
        return BeginRaytracingPassInternal(-1);
    }

    public RaytracingPass BeginRaytracingPass(RenderPass dependency)
    {
        var depIndex = FindPassIndex(dependency);
        return BeginRaytracingPassInternal(depIndex);
    }

    private GraphicsPass AllocateBlitPass()
    {
        var pass = _graphicsPasses[_currentFrame][_graphicsPassCount];
        var semaphore = _graphicsSemaphores[_currentFrame][_graphicsPassCount];
        _graphicsPassCount++;

        pass.Reset();

        ref var passData = ref _passes[_passCount];
        passData.Pass = pass;
        passData.SignalSemaphore = semaphore;
        passData.DependencyCount = 0;

        for (var i = 0; i < _passCount && passData.DependencyCount < MaxDependenciesPerPass; i++)
        {
            AddDependency(_passCount, i);
        }

        _passCount++;
        return pass;
    }

    private GraphicsPass BeginGraphicsPassInternal(int dependencyIndex)
    {
        var pass = _graphicsPasses[_currentFrame][_graphicsPassCount];
        var semaphore = _graphicsSemaphores[_currentFrame][_graphicsPassCount];
        _graphicsPassCount++;

        pass.Reset();

        ref var passData = ref _passes[_passCount];
        passData.Pass = pass;
        passData.SignalSemaphore = semaphore;
        passData.DependencyCount = 0;

        if (dependencyIndex >= 0)
        {
            AddDependency(_passCount, dependencyIndex);
        }

        _passCount++;
        return pass;
    }

    private ComputePass BeginComputePassInternal(int dependencyIndex)
    {
        var pass = _computePasses[_currentFrame][_computePassCount];
        var semaphore = _computeSemaphores[_currentFrame][_computePassCount];
        _computePassCount++;

        pass.Reset();

        ref var passData = ref _passes[_passCount];
        passData.Pass = pass;
        passData.SignalSemaphore = semaphore;
        passData.DependencyCount = 0;

        if (dependencyIndex >= 0)
        {
            AddDependency(_passCount, dependencyIndex);
        }

        _passCount++;
        return pass;
    }

    private RaytracingPass BeginRaytracingPassInternal(int dependencyIndex)
    {
        var pass = _raytracingPasses[_currentFrame][_raytracingPassCount];
        var semaphore = _raytracingSemaphores[_currentFrame][_raytracingPassCount];
        _raytracingPassCount++;

        pass.Reset();

        ref var passData = ref _passes[_passCount];
        passData.Pass = pass;
        passData.SignalSemaphore = semaphore;
        passData.DependencyCount = 0;

        if (dependencyIndex >= 0)
        {
            AddDependency(_passCount, dependencyIndex);
        }

        _passCount++;
        return pass;
    }

    private void AddDependency(int passIndex, int dependencyIndex)
    {
        ref var passData = ref _passes[passIndex];
        if (passData.DependencyCount >= MaxDependenciesPerPass)
        {
            throw new InvalidOperationException("Too many dependencies for pass");
        }

        passData.SetDependency(passData.DependencyCount++, dependencyIndex);
    }

    private int FindPassIndex(RenderPass pass)
    {
        for (var i = 0; i < _passCount; i++)
        {
            if (_passes[i].Pass == pass)
            {
                return i;
            }
        }

        throw new InvalidOperationException("Pass not found in frame");
    }

    public void Submit()
    {
        for (var i = 0; i < _passCount; i++)
        {
            ref var passData = ref _passes[i];

            _submitCommandLists[0] = passData.Pass.CommandList;
            _submitSignalSemaphores[0] = passData.SignalSemaphore;

            for (var j = 0; j < passData.DependencyCount; j++)
            {
                var depIndex = passData.GetDependency(j);
                _submitWaitSemaphores[j] = _passes[depIndex].SignalSemaphore;
            }

            GraphicsContext.GraphicsCommandQueue.ExecuteCommandLists(
                new ReadOnlySpan<CommandList>(_submitCommandLists, 0, 1),
                null,
                new ReadOnlySpan<Semaphore>(_submitWaitSemaphores, 0, passData.DependencyCount),
                new ReadOnlySpan<Semaphore>(_submitSignalSemaphores, 0, 1));
        }
    }

    public void Present(Texture sourceTexture)
    {
        var commandList = _presentCommandLists[_currentFrame];
        var semaphore = _presentSemaphores[_currentFrame];

        _presentPasses[_currentFrame].Execute(commandList, sourceTexture);

        _submitCommandLists[0] = commandList;
        _submitSignalSemaphores[0] = semaphore;

        var waitCount = 0;
        for (var i = 0; i < _passCount && waitCount < MaxDependenciesPerPass; i++)
        {
            _submitWaitSemaphores[waitCount++] = _passes[i].SignalSemaphore;
        }

        GraphicsContext.GraphicsCommandQueue.ExecuteCommandLists(
            new ReadOnlySpan<CommandList>(_submitCommandLists, 0, 1),
            _frameFences[_currentFrame],
            new ReadOnlySpan<Semaphore>(_submitWaitSemaphores, 0, waitCount),
            new ReadOnlySpan<Semaphore>(_submitSignalSemaphores, 0, 1));

        GraphicsContext.SwapChain.Present((uint)_currentFrame);
    }

    public void Present(CycledTexture sourceTexture)
    {
        Present(sourceTexture[GraphicsContext.FrameIndex]);
    }

    public void Dispose()
    {
        GraphicsContext.GraphicsCommandQueue.WaitIdle();

        DisposeBlitResources();
        DisposeUi();
        DisposeDebugOverlay();

        var numFrames = (int)GraphicsContext.NumFrames;
        for (var frame = 0; frame < numFrames; frame++)
        {
            for (var i = 0; i < MaxPassesPerFrame; i++)
            {
                _graphicsPasses[frame][i].Dispose();
                _computePasses[frame][i].Dispose();
                _raytracingPasses[frame][i].Dispose();
            }

            _presentPasses[frame].Dispose();
        }

        foreach (var fence in _frameFences)
        {
            fence.Dispose();
        }

        _commandListAllocator.Dispose();
    }
}
