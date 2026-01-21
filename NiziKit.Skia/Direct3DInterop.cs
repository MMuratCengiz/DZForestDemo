using DenOfIz;
// ReSharper disable InconsistentNaming

namespace NiziKit.Skia;

public static class Direct3DInterop
{
    // D3D12_RESOURCE_STATES
    public const uint D3D12_RESOURCE_STATE_COMMON = 0;
    public const uint D3D12_RESOURCE_STATE_RENDER_TARGET = 0x4;
    public const uint D3D12_RESOURCE_STATE_UNORDERED_ACCESS = 0x8;
    public const uint D3D12_RESOURCE_STATE_DEPTH_WRITE = 0x10;
    public const uint D3D12_RESOURCE_STATE_DEPTH_READ = 0x20;
    public const uint D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE = 0x80;
    public const uint D3D12_RESOURCE_STATE_COPY_DEST = 0x400;
    public const uint D3D12_RESOURCE_STATE_COPY_SOURCE = 0x800;

    /// <summary>
    /// Converts a DenOfIz Format to DXGI_FORMAT.
    /// </summary>
    public static uint FormatToDxgi(Format format)
    {
        return format switch
        {
            Format.R8Unorm => 61,              // DXGI_FORMAT_R8_UNORM
            Format.R8G8Unorm => 49,            // DXGI_FORMAT_R8G8_UNORM
            Format.R8G8B8A8Unorm => 28,        // DXGI_FORMAT_R8G8B8A8_UNORM
            Format.R8G8B8A8UnormSrgb => 29,    // DXGI_FORMAT_R8G8B8A8_UNORM_SRGB
            Format.B8G8R8A8Unorm => 87,        // DXGI_FORMAT_B8G8R8A8_UNORM
            Format.R10G10B10A2Unorm => 24,     // DXGI_FORMAT_R10G10B10A2_UNORM
            Format.R16Float => 54,             // DXGI_FORMAT_R16_FLOAT
            Format.R16G16Float => 34,          // DXGI_FORMAT_R16G16_FLOAT
            Format.R16G16B16A16Float => 10,    // DXGI_FORMAT_R16G16B16A16_FLOAT
            Format.R32Float => 41,             // DXGI_FORMAT_R32_FLOAT
            Format.R32G32Float => 16,          // DXGI_FORMAT_R32G32_FLOAT
            Format.R32G32B32Float => 6,        // DXGI_FORMAT_R32G32B32_FLOAT
            Format.R32G32B32A32Float => 2,     // DXGI_FORMAT_R32G32B32A32_FLOAT
            Format.D16Unorm => 55,             // DXGI_FORMAT_D16_UNORM
            Format.D32Float => 40,             // DXGI_FORMAT_D32_FLOAT
            Format.D24UnormS8Uint => 45,       // DXGI_FORMAT_D24_UNORM_S8_UINT
            _ => throw new NotSupportedException($"Format {format} is not supported for DXGI conversion")
        };
    }
}
