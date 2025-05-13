// See https://aka.ms/new-console-template for more information

using DenOfIz;

var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
InteropString targetDirectory = new("C:/Workspace/DZForestDemo/DZForestDemo/Assets/MeadowForest/");
InteropString importRoot =
    new($"{home}/Downloads/Polygon_NatureBiomes_MeadowForest_SourceFiles_v2/Meadow_Source_Files/");
var texRoot = importRoot.Append("Textures/");
var fbxRoot = importRoot.Append("FBX/");

EngineDesc engineDesc = new();
engineDesc.FS.AssetPath = new InteropString("C:/Workspace/DZForestDemo/DZForestDemo/Assets");
DenOfIzGraphicsInitializer.Initialize(engineDesc);

AssimpImporterDesc importerDesc = new();
AssimpImporter importer = new(importerDesc);

ImportJobDesc importJobDesc = new();
importJobDesc.SourceFilePath = fbxRoot.Append("SM_Env_Tree_Meadow_01.fbx");
importJobDesc.AssetNamePrefix = new InteropString("");
importJobDesc.TargetDirectory = targetDirectory.Append("Trees/Meadow/");

importJobDesc.Desc = new AssimpImportDesc();

var result = importer.Import(importJobDesc);
if (result.ResultCode != ImporterResultCode.Success)
{
    Console.WriteLine($"Error {result.ResultCode}: {result.ErrorMessage.Get()}");
}