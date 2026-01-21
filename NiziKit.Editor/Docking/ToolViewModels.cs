using Dock.Model.Mvvm.Controls;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Docking;

public class SceneTreeToolViewModel : Tool
{
    public EditorViewModel? EditorViewModel { get; set; }
}

public class InspectorToolViewModel : Tool
{
    public EditorViewModel? EditorViewModel { get; set; }
}

public class AssetBrowserToolViewModel : Tool
{
    public EditorViewModel? EditorViewModel { get; set; }
}

public class PackManagerToolViewModel : Tool
{
    public EditorViewModel? EditorViewModel { get; set; }
}
