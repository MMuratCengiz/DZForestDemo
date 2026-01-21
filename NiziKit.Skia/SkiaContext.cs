using DenOfIz;
using NiziKit.Graphics;
using SkiaSharp;

namespace NiziKit.Skia;

/// <summary>
/// Manages the SkiaSharp GRContext created from DenOfIz device handles.
/// Provides GPU-accelerated Skia rendering using the same underlying graphics device as DenOfIz.
/// </summary>
public sealed class SkiaContext : IDisposable
{
    private static SkiaContext? _instance;
    public static SkiaContext Instance => _instance ?? throw new InvalidOperationException("SkiaContext not initialized");

    public static GRContext GRContext => Instance._grContext;

    private readonly GRContext _grContext;
    private readonly GRMtlBackendContext? _mtlBackendContext;
    private readonly GRVkBackendContext? _vkBackendContext;
    private bool _disposed;

    /// <summary>
    /// Creates a SkiaContext from a DenOfIz LogicalDevice and CommandQueue.
    /// </summary>
    /// <param name="device">The DenOfIz logical device.</param>
    /// <param name="graphicsQueue">The DenOfIz graphics command queue (needed for Metal backend).</param>
    public SkiaContext(LogicalDevice device, CommandQueue graphicsQueue)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(graphicsQueue);

        var deviceHandles = NativeInterop.GetNativeDeviceHandles(device);
        var queueHandles = NativeInterop.GetNativeQueueHandles(graphicsQueue);

        (_grContext, _mtlBackendContext, _vkBackendContext) = CreateGRContext(deviceHandles, queueHandles);
        _instance = this;
    }

    /// <summary>
    /// Creates a SkiaContext using the GraphicsContext singleton.
    /// </summary>
    public SkiaContext() : this(GraphicsContext.Device, GraphicsContext.GraphicsQueue)
    {
    }

    private static (GRContext context, GRMtlBackendContext? mtl, GRVkBackendContext? vk) CreateGRContext(
        NativeDeviceHandles deviceHandles,
        NativeQueueHandles queueHandles)
    {
        switch (deviceHandles.Backend)
        {
            case GraphicsBackendType.Metal:
                return CreateMetalContext(deviceHandles, queueHandles);

            case GraphicsBackendType.Vulkan:
                return CreateVulkanContext(deviceHandles, queueHandles);

            default:
                throw new NotSupportedException(
                    $"Graphics backend {deviceHandles.Backend} is not supported for Skia interop. " +
                    "Supported backends: Metal, Vulkan.");
        }
    }

    private static (GRContext, GRMtlBackendContext?, GRVkBackendContext?) CreateMetalContext(
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

        return (grContext, mtlContext, null);
    }

    private static (GRContext, GRMtlBackendContext?, GRVkBackendContext?) CreateVulkanContext(
        NativeDeviceHandles deviceHandles,
        NativeQueueHandles queueHandles)
    {
        if (deviceHandles.VkDevice == IntPtr.Zero ||
            deviceHandles.VkPhysicalDevice == IntPtr.Zero ||
            deviceHandles.VkInstance == IntPtr.Zero)
        {
            throw new InvalidOperationException("Vulkan device handles are incomplete");
        }

        var vkContext = new GRVkBackendContext
        {
            VkInstance = deviceHandles.VkInstance,
            VkPhysicalDevice = deviceHandles.VkPhysicalDevice,
            VkDevice = deviceHandles.VkDevice,
            VkQueue = queueHandles.VkQueue,
            GraphicsQueueIndex = queueHandles.VkQueueFamilyIndex,
            GetProcedureAddress = CreateVkGetProcDelegate(deviceHandles.VkGetInstanceProcAddr)
        };

        var grContext = GRContext.CreateVulkan(vkContext);
        if (grContext == null)
        {
            throw new InvalidOperationException("Failed to create Vulkan GRContext");
        }

        return (grContext, null, vkContext);
    }

    private static GRVkGetProcedureAddressDelegate CreateVkGetProcDelegate(IntPtr getProcAddr)
    {
        // Create a delegate that wraps the native vkGetInstanceProcAddr
        return (name, instance, device) =>
        {
            // For simplicity, we use the instance proc addr for all lookups
            // A more complete implementation would use vkGetDeviceProcAddr for device-level functions
            unsafe
            {
                var funcPtr = (delegate* unmanaged<IntPtr, byte*, IntPtr>)getProcAddr;
                var nameBytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
                fixed (byte* namePtr = nameBytes)
                {
                    return funcPtr(instance, namePtr);
                }
            }
        };
    }

    /// <summary>
    /// Creates a Skia surface that can be used for off-screen rendering.
    /// The surface uses the GPU context for hardware acceleration.
    /// </summary>
    /// <param name="width">Width of the surface in pixels.</param>
    /// <param name="height">Height of the surface in pixels.</param>
    /// <param name="colorType">The color type for the surface. Defaults to BGRA8888.</param>
    /// <returns>A GPU-backed SKSurface.</returns>
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

    /// <summary>
    /// Creates a render target surface suitable for rendering to a texture.
    /// </summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="sampleCount">MSAA sample count. Defaults to 1 (no MSAA).</param>
    /// <returns>A GPU-backed render target surface.</returns>
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

    /// <summary>
    /// Flushes and submits pending Skia GPU operations.
    /// Call this after finishing Skia rendering before using resources in DenOfIz.
    /// </summary>
    public void Flush()
    {
        ThrowIfDisposed();
        _grContext.Flush();
    }

    /// <summary>
    /// Flushes Skia operations and waits for GPU completion.
    /// Use when you need to ensure all Skia operations are complete before proceeding.
    /// </summary>
    public void FlushAndSubmit(bool syncCpu = false)
    {
        ThrowIfDisposed();
        _grContext.Flush();
        _grContext.Submit(syncCpu);
    }

    /// <summary>
    /// Resets the GRContext, releasing any cached resources.
    /// Useful when memory pressure is high.
    /// </summary>
    public void ResetContext()
    {
        ThrowIfDisposed();
        _grContext.ResetContext();
    }

    /// <summary>
    /// Purges unused GPU resources from the context cache.
    /// </summary>
    public void PurgeResources()
    {
        ThrowIfDisposed();
        _grContext.PurgeResources();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _grContext.Dispose();
        _mtlBackendContext?.Dispose();

        if (_instance == this)
        {
            _instance = null;
        }
    }
}
