using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NiziKit.Assets;
using NiziKit.Components;
using NiziKit.Core;
using NiziKit.Editor.ViewModels;
using NiziKit.Editor.Views.Editors;
using NiziKit.Graphics.Renderer.Forward;

namespace NiziKit.Editor.Views;

public partial class SceneSettingsPanel : UserControl
{
    private StackPanel? _skyboxFacesPanel;

    public SceneSettingsPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _skyboxFacesPanel = this.FindControl<StackPanel>("SkyboxFacesPanel");
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        BuildSkyboxEditors();
    }

    protected override void OnPropertyChanged(Avalonia.AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsVisibleProperty && change.NewValue is true)
        {
            BuildSkyboxEditors();
        }
    }

    private void BuildSkyboxEditors()
    {
        if (_skyboxFacesPanel == null || DataContext is not EditorViewModel vm)
        {
            return;
        }

        _skyboxFacesPanel.Children.Clear();

        var scene = World.CurrentScene;
        var skybox = scene?.Skybox;

        var faces = new (string Label, string? CurrentRef, Texture2d? CurrentTex, Action<Texture2d?, string?> Setter)[]
        {
            ("Right", skybox?.RightRef, skybox?.Right, (tex, r) => { if (skybox != null) { skybox.Right = tex; skybox.RightRef = r; } }),
            ("Left", skybox?.LeftRef, skybox?.Left, (tex, r) => { if (skybox != null) { skybox.Left = tex; skybox.LeftRef = r; } }),
            ("Up", skybox?.UpRef, skybox?.Up, (tex, r) => { if (skybox != null) { skybox.Up = tex; skybox.UpRef = r; } }),
            ("Down", skybox?.DownRef, skybox?.Down, (tex, r) => { if (skybox != null) { skybox.Down = tex; skybox.DownRef = r; } }),
            ("Front", skybox?.FrontRef, skybox?.Front, (tex, r) => { if (skybox != null) { skybox.Front = tex; skybox.FrontRef = r; } }),
            ("Back", skybox?.BackRef, skybox?.Back, (tex, r) => { if (skybox != null) { skybox.Back = tex; skybox.BackRef = r; } }),
        };

        foreach (var (label, currentRef, currentTex, setter) in faces)
        {
            var editor = new AssetRefEditor
            {
                AssetType = AssetRefType.Texture,
                AssetBrowser = vm.AssetBrowser,
                EditorViewModel = vm,
                CurrentAsset = currentTex,
                OnAssetChanged = (asset, assetRef) =>
                {
                    EnsureSkybox();
                    var tex = asset as Texture2d;
                    if (tex == null && !string.IsNullOrEmpty(assetRef))
                    {
                        tex = new Texture2d();
                        tex.Load(assetRef);
                    }
                    setter(tex, assetRef);
                }
            };

            var grid = new Grid
            {
                ColumnDefinitions = ColumnDefinitions.Parse("70,*")
            };

            var labelBlock = new TextBlock
            {
                Text = label,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            labelBlock.SetValue(TextBlock.ForegroundProperty,
                this.FindResource("EditorTextSecondary") as Avalonia.Media.IBrush);
            Grid.SetColumn(labelBlock, 0);
            Grid.SetColumn(editor, 1);

            grid.Children.Add(labelBlock);
            grid.Children.Add(editor);

            _skyboxFacesPanel.Children.Add(grid);
        }
    }

    private static void EnsureSkybox()
    {
        var scene = World.CurrentScene;
        if (scene is { Skybox: null })
        {
            scene.Skybox = new SkyboxData();
        }
    }
}
