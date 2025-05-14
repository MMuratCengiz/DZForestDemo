// See https://aka.ms/new-console-template for more information

Console.WriteLine("Hello, World!");

GraphicsApi graphicsApi = new GraphicsApi(new APIPreference());
ILogicalDevice logicalDevice = graphicsApi.CreateAndLoadOptimalLogicalDevice();
