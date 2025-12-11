using DenOfIz;

namespace DZForestDemo;

internal static class Program
{
    
    private static void Main(string[] args)
    {
        DenOfIzRuntime.Initialize();
        Engine.Init(new EngineDesc());

        var preference = new APIPreference
        {
            Windows = APIPreferenceWindows.Directx12
        };
        var graphicsApi = new GraphicsApi(preference);
        var logicalDevice = graphicsApi.CreateAndLoadOptimalLogicalDevice(new LogicalDeviceDesc());

        using var renderLoop = new WindowRenderLoop(logicalDevice, 1920, 1080, "DenOfIz Window Mode");

        while (renderLoop.PollAndRender())
        {
        }

        logicalDevice.WaitIdle();
        logicalDevice.Dispose();
        GraphicsApi.ReportLiveObjects();
        Engine.Shutdown();
    }
}
