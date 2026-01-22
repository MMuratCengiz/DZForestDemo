using DenOfIz;
// ReSharper disable InconsistentNaming

namespace NiziKit.Skia;

public static class VulkanInterop
{
    public const uint VK_IMAGE_TILING_OPTIMAL = 0;
    public const uint VK_SHARING_MODE_EXCLUSIVE = 0;
    public const uint VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL = 2;
    public const uint VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL = 5;
    public const uint VK_IMAGE_USAGE_TRANSFER_SRC_BIT = 0x00000001;
    public const uint VK_IMAGE_USAGE_TRANSFER_DST_BIT = 0x00000002;
    public const uint VK_IMAGE_USAGE_SAMPLED_BIT = 0x00000004;
    public const uint VK_IMAGE_USAGE_COLOR_ATTACHMENT_BIT = 0x00000010;
    public const uint VK_QUEUE_FAMILY_IGNORED = uint.MaxValue;

    public static uint FormatToVulkan(Format format)
    {
        return format switch
        {
            Format.R8Unorm => 9,
            Format.R8G8Unorm => 16,
            Format.R8G8B8A8Unorm => 37,
            Format.R8G8B8A8UnormSrgb => 43,
            Format.B8G8R8A8Unorm => 44,
            Format.R10G10B10A2Unorm => 64,
            Format.R16Float => 76,
            Format.R16G16Float => 83,
            Format.R16G16B16A16Float => 97,
            Format.R32Float => 100,
            Format.R32G32Float => 103,
            Format.R32G32B32Float => 106,
            Format.R32G32B32A32Float => 109,
            Format.D16Unorm => 124,
            Format.D32Float => 126,
            Format.D24UnormS8Uint => 129,
            _ => throw new NotSupportedException($"Format {format} is not supported for Vulkan conversion")
        };
    }
}
