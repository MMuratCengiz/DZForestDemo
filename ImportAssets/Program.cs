// See https://aka.ms/new-console-template for more information

using DenOfIz;

EngineDesc engineDesc = new();
engineDesc.FS.AssetPath = new InteropString("C:/Workspace/DZForestDemo/DZForestDemo/Assets");
DenOfIzGraphicsInitializer.Initialize(engineDesc);

AssimpImporterDesc importerDesc = new();
AssimpImporter importer = new(importerDesc);

