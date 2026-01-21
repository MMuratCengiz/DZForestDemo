using DenOfIz;
using NiziKit.Graphics;
using SkiaSharp;
using Semaphore = DenOfIz.Semaphore;

namespace NiziKit.Skia;

public sealed class SkiaContext : IDisposable
{
    private static SkiaContext? _instance;
    public static SkiaContext Instance => _instance ?? throw new InvalidOperationException("SkiaContext not initialized");
    public static GRContext GRContext => Instance._grContext;

    private readonly GRContext _grContext;
    private readonly GRMtlBackendContext? _mtlBackendContext;
    private readonly GRVkBackendContext? _vkBackendContext;
    private readonly GRD3DBackendContext? _d3dBackendContext;
    private readonly CommandListPool _commandListPool;
    private readonly CommandList[] _commandLists;
    private int _currentCommandListIndex;
    private bool _disposed;

    public SkiaContext(LogicalDevice device, CommandQueue graphicsQueue)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(graphicsQueue);

        var deviceHandles = NativeInterop.GetNativeDeviceHandles(device);
        var queueHandles = NativeInterop.GetNativeQueueHandles(graphicsQueue);

        (_grContext, _mtlBackendContext, _vkBackendContext, _d3dBackendContext) = CreateGRContext(deviceHandles, queueHandles);

        _commandListPool = device.CreateCommandListPool(new CommandListPoolDesc
        {
            CommandQueue = graphicsQueue,
            NumCommandLists = 4
        });
        _commandLists = _commandListPool.GetCommandLists().ToArray();
        _currentCommandListIndex = 0;

        _instance = this;
    }

    public SkiaContext() : this(GraphicsContext.Device, GraphicsContext.GraphicsQueue)
    {
    }

    private static (GRContext context, GRMtlBackendContext? mtl, GRVkBackendContext? vk, GRD3DBackendContext? d3d) CreateGRContext(
        NativeDeviceHandles deviceHandles,
        NativeQueueHandles queueHandles)
    {
        return deviceHandles.Backend switch
        {
            GraphicsBackendType.Metal => CreateMetalContext(deviceHandles, queueHandles),
            GraphicsBackendType.Vulkan => CreateVulkanContext(deviceHandles, queueHandles),
            GraphicsBackendType.Directx12 => CreateDirect3DContext(deviceHandles, queueHandles),
            _ => throw new NotSupportedException(
                $"Graphics backend {deviceHandles.Backend} is not supported for Skia interop. " +
                "Supported backends: Metal, Vulkan, D3D12.")
        };
    }

    private static (GRContext, GRMtlBackendContext?, GRVkBackendContext?, GRD3DBackendContext?) CreateMetalContext(
        NativeDeviceHandles deviceHandles,
        NativeQueueHandles queueHandles)
    {
        if (deviceHandles.MTLDevice == IntPtr.Zero)
        {
            throw new InvalidOperationException("Metal device handle is null");
        }

        if (queueHandles.MTLCommandQueue == IntPtr.Zero)
        {
            throw new InvalidOperationException("Metal command queue handle is null");
        }

        var mtlContext = new GRMtlBackendContext
        {
            DeviceHandle = deviceHandles.MTLDevice,
            QueueHandle = queueHandles.MTLCommandQueue
        };

        var grContext = GRContext.CreateMetal(mtlContext);
        if (grContext == null)
        {
            mtlContext.Dispose();
            throw new InvalidOperationException("Failed to create Metal GRContext");
        }

        return (grContext, mtlContext, null, null);
    }

    private static (GRContext, GRMtlBackendContext?, GRVkBackendContext?, GRD3DBackendContext?) CreateVulkanContext(
        NativeDeviceHandles deviceHandles,
        NativeQueueHandles queueHandles)
    {
        if (deviceHandles.VkDevice == IntPtr.Zero ||
            deviceHandles.VkPhysicalDevice == IntPtr.Zero ||
            deviceHandles.VkInstance == IntPtr.Zero)
        {
            throw new InvalidOperationException("Vulkan device handles are incomplete");
        }

        if (deviceHandles.VkGetInstanceProcAddr == IntPtr.Zero)
        {
            throw new InvalidOperationException("VkGetInstanceProcAddr is null");
        }

        var vkContext = new GRVkBackendContext
        {
            VkInstance = deviceHandles.VkInstance,
            VkPhysicalDevice = deviceHandles.VkPhysicalDevice,
            VkDevice = deviceHandles.VkDevice,
            VkQueue = queueHandles.VkQueue,
            GraphicsQueueIndex = queueHandles.VkQueueFamilyIndex,
            GetProcedureAddress = CreateVkGetProcDelegate(
                deviceHandles.VkGetInstanceProcAddr,
                deviceHandles.VkGetDeviceProcAddr)
        };

        var grContext = GRContext.CreateVulkan(vkContext);
        if (grContext == null)
        {
            throw new InvalidOperationException("Failed to create Vulkan GRContext");
        }

        return (grContext, null, vkContext, null);
    }

    private static (GRContext, GRMtlBackendContext?, GRVkBackendContext?, GRD3DBackendContext?) CreateDirect3DContext(
        NativeDeviceHandles deviceHandles,
        NativeQueueHandles queueHandles)
    {
        if (deviceHandles.DX12Device == IntPtr.Zero)
        {
            throw new InvalidOperationException("D3D12 device handle is null");
        }

        if (deviceHandles.DX12Adapter == IntPtr.Zero)
        {
            throw new InvalidOperationException("D3D12 adapter handle is null");
        }

        if (queueHandles.DX12CommandQueue == IntPtr.Zero)
        {
            throw new InvalidOperationException("D3D12 command queue handle is null");
        }

        var d3dContext = new GRD3DBackendContext
        {
            Adapter = deviceHandles.DX12Adapter,
            Device = deviceHandles.DX12Device,
            Queue = queueHandles.DX12CommandQueue,
            ProtectedContext = false
        };

        var grContext = GRContext.CreateDirect3D(d3dContext);
        if (grContext == null)
        {
            d3dContext.Dispose();
            throw new InvalidOperationException("Failed to create Direct3D GRContext");
        }

        return (grContext, null, null, d3dContext);
    }

    private static GRVkGetProcedureAddressDelegate CreateVkGetProcDelegate(
        IntPtr vkGetInstanceProcAddr,
        IntPtr vkGetDeviceProcAddr)
    {
        return (name, instance, device) =>
        {
            unsafe
            {
                var nameBytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
                fixed (byte* namePtr = nameBytes)
                {
                    if (device != IntPtr.Zero && vkGetDeviceProcAddr != IntPtr.Zero)
                    {
                        var devProcFunc = (delegate* unmanaged<IntPtr, byte*, IntPtr>)vkGetDeviceProcAddr;
                        var result = devProcFunc(device, namePtr);
                        if (result != IntPtr.Zero)
                        {
                            return result;
                        }
                    }

                    var instProcFunc = (delegate* unmanaged<IntPtr, byte*, IntPtr>)vkGetInstanceProcAddr;
                    return instProcFunc(instance, namePtr);
                }
            }
        };
    }

    public SKSurface CreateSurface(int width, int height, SKColorType colorType = SKColorType.Bgra8888)
    {
        ThrowIfDisposed();

        var imageInfo = new SKImageInfo(width, height, colorType, SKAlphaType.Premul);
        var surface = SKSurface.Create(_grContext, false, imageInfo);

        if (surface == null)
        {
            throw new InvalidOperationException($"Failed to create GPU surface ({width}x{height})");
        }

        return surface;
    }

    public SKSurface CreateRenderTarget(int width, int height, int sampleCount = 1)
    {
        ThrowIfDisposed();

        var imageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var surface = SKSurface.Create(_grContext, false, imageInfo, sampleCount, GRSurfaceOrigin.TopLeft);

        if (surface == null)
        {
            throw new InvalidOperationException($"Failed to create render target surface ({width}x{height}, samples={sampleCount})");
        }

        return surface;
    }

    public void Flush()
    {
        ThrowIfDisposed();
        _grContext.Flush();
    }

    public void FlushAndSubmit(bool syncCpu = false)
    {
        ThrowIfDisposed();
        _grContext.Flush();
        _grContext.Submit(syncCpu);
    }

    public void ResetContext()
    {
        ThrowIfDisposed();
        _grContext.ResetContext();
    }

    public void PurgeResources()
    {
        ThrowIfDisposed();
        _grContext.PurgeResources();
    }

    public void TransitionTextureForRendering(Texture texture, ResourceTracking resourceTracking)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentNullException.ThrowIfNull(resourceTracking);

        var commandList = _commandLists[_currentCommandListIndex];
        _currentCommandListIndex = (_currentCommandListIndex + 1) % _commandLists.Length;

        commandList.Begin();

        resourceTracking.TransitionTexture(
            commandList,
            texture,
            (uint)ResourceUsageFlagBits.RenderTarget,
            QueueType.Graphics);

        commandList.End();

        GraphicsContext.GraphicsQueue.ExecuteCommandLists(
            new ReadOnlySpan<CommandList>(ref commandList),
            null,
            ReadOnlySpan<Semaphore>.Empty,
            ReadOnlySpan<Semaphore>.Empty);

        GraphicsContext.GraphicsQueue.WaitIdle();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _grContext.Dispose();
        _mtlBackendContext?.Dispose();
        _d3dBackendContext?.Dispose();
        _commandListPool.Dispose();

        if (_instance == this)
        {
            _instance = null;
        }
    }
}
