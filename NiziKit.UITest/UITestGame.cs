using DenOfIz;
using NiziKit.Application;
using NiziKit.Application.Timing;
using NiziKit.Graphics;
using NiziKit.Graphics.Renderer;
using NiziKit.Graphics.Renderer.Forward;
using NiziKit.UI;

namespace NiziKit.UITest;

public sealed class UITestGame(GameDesc? desc = null) : Game(desc)
{
    private RenderFrame _renderFrame = null!;

    private int _clickCount;
    private bool _checkboxA = true;
    private bool _checkboxB;
    private string _textFieldValue = "Hello World";
    private string _multilineText = "Line one\nLine two\nLine three";
    private int _dropdownIndex;
    private readonly string[] _dropdownItems = ["Option A", "Option B", "Option C", "Option D"];
    private int _activeTab;
    private readonly string[] _tabs = ["Widgets", "Layout", "Stress Test", "New Widgets"];

    private float _sliderValue = 0.5f;
    private float _sliderValue2 = 50f;
    private float _dragValue = 1.0f;
    private float _posX, _posY, _posZ;
    private string _treeSelectedId = "";
    private readonly List<UiTreeNode> _treeNodes = BuildSceneTree();
    private readonly UiContextMenuItem[] _contextMenuItems =
    [
        UiContextMenuItem.Item("Cut", FontAwesome.Cut),
        UiContextMenuItem.Item("Copy", FontAwesome.Copy),
        UiContextMenuItem.Item("Paste", FontAwesome.Paste),
        UiContextMenuItem.Separator(),
        UiContextMenuItem.Item("Delete", FontAwesome.Trash),
        new() { Label = "Disabled", IsDisabled = true }
    ];
    private float _propFloat = 3.14f;
    private float _propR = 1.0f, _propG = 0.5f, _propB = 0.0f;
    private int _listSelectedIndex;
    private List<string> _listItems = ["Item Alpha", "Item Beta", "Item Gamma", "Item Delta"];

    public override Type RendererType => typeof(ForwardRenderer);

    protected override void Load(Game game)
    {
        _renderFrame = new RenderFrame();
        _renderFrame.EnableDebugOverlay(DebugOverlayConfig.Default);
        _renderFrame.EnableUi(UiContextDesc.Default);

        FontAwesome.InitializeEmbedded(_renderFrame.UiContext.Clay);
    }

    protected override void OnEvent(ref Event ev)
    {
        _renderFrame.HandleUiEvent(ev);
        _renderFrame.UiContext.RecordEvent(ev);
    }

    protected override void Update(float dt)
    {
        _renderFrame.BeginFrame();

        var debugOverlay = _renderFrame.RenderDebugOverlay();
        var ui = _renderFrame.RenderUi(BuildUi);

        _renderFrame.AlphaBlit(ui, debugOverlay);

        _renderFrame.Submit();
        _renderFrame.Present(debugOverlay);

        _renderFrame.UiContext.ClearFrameEvents();
    }

    private void BuildUi(UiFrame ui)
    {
        var ctx = _renderFrame.UiContext;

        using var root = ui.Root()
            .Background(UiColor.Rgb(30, 30, 34))
            .Vertical()
            .Padding(0)
            .Gap(0)
            .Open();

        using (ui.Panel("TitleBar")
                   .Horizontal()
                   .GrowWidth()
                   .FitHeight()
                   .Background(UiColor.Rgb(20, 20, 24))
                   .Padding(16, 10)
                   .AlignChildren(UiAlignX.Left, UiAlignY.Center)
                   .Gap(12)
                   .Open())
        {
            root.Icon(FontAwesome.Cubes, UiColor.Rgb(100, 200, 130), 20);

            root.Text("NiziKit UI Test", new UiTextStyle
            {
                Color = UiColor.Rgb(100, 200, 130),
                FontSize = 20,
                Alignment = UiTextAlign.Left
            });

            Ui.FlexSpacer(ctx);

            root.Icon(FontAwesome.Clock, UiColor.Rgb(180, 180, 180), 14);
            root.Text($"FPS: {Time.FramesPerSecond:F0}", new UiTextStyle
            {
                Color = UiColor.Rgb(180, 180, 180),
                FontSize = 14,
            });

            root.Icon(FontAwesome.Gear, UiColor.Rgb(150, 150, 150), 16);
        }

        using (ui.Panel("TabBar")
                   .Horizontal()
                   .GrowWidth()
                   .FitHeight()
                   .Background(UiColor.Rgb(25, 25, 30))
                   .Padding(8, 4)
                   .Gap(4)
                   .Open())
        {
            for (var i = 0; i < _tabs.Length; i++)
            {
                var isActive = i == _activeTab;
                var tabColor = isActive ? UiColor.Rgb(64, 128, 96) : UiColor.Rgb(45, 45, 50);
                var tabBtn = Ui.Button(ctx, $"Tab{i}", _tabs[i])
                    .Color(tabColor)
                    .FontSize(14)
                    .Padding(12, 6)
                    .CornerRadius(4);

                if (tabBtn.Show())
                {
                    _activeTab = i;
                }
            }
        }

        using (ui.Panel("Content")
                   .Grow()
                   .Padding(16)
                   .Gap(12)
                   .Vertical()
                   .ScrollVertical()
                   .Open())
        {
            switch (_activeTab)
            {
                case 0:
                    BuildWidgetsTab(ui, ctx);
                    break;
                case 1:
                    BuildLayoutTab(ui, ctx);
                    break;
                case 2:
                    BuildStressTestTab(ui, ctx);
                    break;
                case 3:
                    BuildNewWidgetsTab(ui, ctx);
                    break;
            }
        }
    }

    private void BuildWidgetsTab(UiFrame ui, UiContext ctx)
    {
        SectionHeader(ui, "FontAwesome Icons");

        using (ui.Row("IconsRow").Gap(16).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
        {
            ui.Icon(FontAwesome.Home, UiColor.Rgb(100, 200, 130), 20);
            ui.Icon(FontAwesome.User, UiColor.Rgb(100, 149, 237), 20);
            ui.Icon(FontAwesome.Gear, UiColor.Rgb(200, 150, 100), 20);
            ui.Icon(FontAwesome.Search, UiColor.Rgb(180, 100, 180), 20);
            ui.Icon(FontAwesome.Bell, UiColor.Rgb(200, 200, 100), 20);
            ui.Icon(FontAwesome.Heart, UiColor.Rgb(220, 80, 80), 20);
            ui.Icon(FontAwesome.Star, UiColor.Rgb(255, 200, 50), 20);
            ui.Icon(FontAwesome.Check, UiColor.Rgb(100, 200, 100), 20);
            ui.Icon(FontAwesome.Xmark, UiColor.Rgb(200, 80, 80), 20);
            ui.Icon(FontAwesome.Plus, UiColor.Rgb(100, 180, 220), 20);
            ui.Icon(FontAwesome.Folder, UiColor.Rgb(220, 180, 80), 20);
            ui.Icon(FontAwesome.File, UiColor.Rgb(180, 180, 180), 20);
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Buttons");

        using (ui.Row("ButtonRow").Gap(8).FitHeight().GrowWidth().Open())
        {
            if (Ui.Button(ctx, "BtnDefault", "Default").Show())
            {
                _clickCount++;
            }

            if (Ui.Button(ctx, "BtnPrimary", "Primary")
                .Color(UiColor.Rgb(60, 130, 200))
                .Show())
            {
                _clickCount++;
            }

            if (Ui.Button(ctx, "BtnDanger", "Danger")
                .Color(UiColor.Rgb(200, 60, 60))
                .Show())
            {
                _clickCount++;
            }

            Ui.Button(ctx, "BtnGhost", "Ghost")
                .Color(UiColor.Transparent, UiColor.Rgba(255, 255, 255, 20), UiColor.Rgba(255, 255, 255, 10))
                .Border(1, UiColor.Rgb(70, 70, 75))
                .Show();

            Ui.HorizontalSpacer(ctx, 16);

            ui.Text($"Clicks: {_clickCount}", UiTextStyle.Default.WithColor(UiColor.Rgb(180, 180, 180)));
        }

        Ui.VerticalSpacer(ctx, 8);

        using (ui.Row("CustomBtnRow").Gap(8).FitHeight().GrowWidth().Open())
        {
            if (Ui.Button(ctx, "BtnLarge", "Large Button")
                    .Color(UiColor.Rgb(64, 128, 96))
                    .FontSize(18)
                    .Padding(24, 12)
                    .CornerRadius(8)
                    .Show())
            {
                _clickCount++;
            }

            if (Ui.Button(ctx, "BtnSmall", "Small")
                    .FontSize(11)
                    .Padding(8, 4)
                    .CornerRadius(3)
                    .Show())
            {
                _clickCount++;
            }
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Checkboxes");

        using (ui.Row("CheckboxRow").Gap(16).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
        {
            if (Ui.Checkbox(ctx, "ChkA", "Enable feature A", _checkboxA)
                    .FontSize(14)
                    .BoxSize(18)
                    .CheckColor(UiColor.Rgb(64, 128, 96))
                    .Show())
            {
                _checkboxA = !_checkboxA;
            }

            if (Ui.Checkbox(ctx, "ChkB", "Enable feature B", _checkboxB)
                .FontSize(14)
                .BoxSize(18)
                .CheckColor(UiColor.Rgb(60, 130, 200))
                .Show())
            {
                _checkboxB = !_checkboxB;
            }

            ui.Text($"A={_checkboxA}, B={_checkboxB}", UiTextStyle.Default.WithColor(UiColor.Gray).WithSize(12));
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Text Fields");

        using (ui.Column("TextFieldCol").Gap(8).FitHeight().GrowWidth().Open())
        {
            using (ui.Row("TFRow1").Gap(8).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
            {
                ui.Text("Single line:", UiTextStyle.Default.WithSize(14));
                Ui.TextField(ctx, "TF1", ref _textFieldValue)
                    .Width(UiSizing.Grow())
                    .FontSize(14)
                    .Placeholder("Type here...")
                    .Show(ref _textFieldValue, Time.DeltaTime);
            }

            ui.Text("Multi-line:", UiTextStyle.Default.WithSize(14));
            Ui.TextField(ctx, "TFMulti", ref _multilineText)
                .Width(UiSizing.Grow())
                .Height(100)
                .FontSize(14)
                .Multiline()
                .Placeholder("Multi-line input...")
                .Show(ref _multilineText, Time.DeltaTime);
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Dropdown");

        using (ui.Row("DropdownRow").Gap(8).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
        {
            ui.Text("Selection:", UiTextStyle.Default.WithSize(14));
            Ui.Dropdown(ctx, "DD1", _dropdownItems)
                .Width(200)
                .FontSize(14)
                .Show(ref _dropdownIndex);

            ui.Text($"Selected: {_dropdownItems[Math.Max(0, _dropdownIndex)]}", UiTextStyle.Default.WithColor(UiColor.Gray).WithSize(12));
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Cards");

        using (ui.Row("CardRow").Gap(12).FitHeight().GrowWidth().Open())
        {
            using (Ui.Card(ctx, "Card1")
                       .Background(UiColor.Rgb(40, 40, 45))
                       .Border(1, UiColor.Rgb(60, 60, 65))
                       .Padding(16)
                       .Gap(8)
                       .GrowWidth()
                       .Open())
            {
                ui.Text("Card Title", UiTextStyle.Default.WithSize(16).WithColor(UiColor.Rgb(100, 200, 130)));
                ui.Text("This is a card component with background, border, padding, and gap.", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));
                Ui.Divider(ctx, UiColor.Rgb(60, 60, 65));
                ui.Text("Footer content", UiTextStyle.Default.WithSize(12).WithColor(UiColor.Gray));
            }

            using (Ui.Card(ctx, "Card2")
                       .Background(UiColor.Rgb(40, 40, 45))
                       .Border(1, UiColor.Rgb(60, 60, 65))
                       .Padding(16)
                       .Gap(8)
                       .GrowWidth()
                       .Open())
            {
                ui.Text("Another Card", UiTextStyle.Default.WithSize(16).WithColor(UiColor.Rgb(100, 149, 237)));
                ui.Text("Cards can hold any combination of widgets and layout elements.", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));

                if (Ui.Button(ctx, "CardBtn", "Card Action")
                        .Color(UiColor.Rgb(60, 130, 200))
                        .FontSize(13)
                        .GrowWidth()
                        .Show())
                {
                    _clickCount++;
                }
            }
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Dividers & Spacers");

        using (ui.Column("DividerCol").Gap(8).FitHeight().GrowWidth().Open())
        {
            ui.Text("Above divider", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));
            Ui.Divider(ctx, UiColor.Rgb(80, 80, 85), 2);
            ui.Text("Below divider", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));

            using (ui.Row("VDivRow").Gap(8).FitHeight(0, 40).GrowWidth().Open())
            {
                ui.Text("Left", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));
                Ui.VerticalDivider(ctx, UiColor.Rgb(80, 80, 85), 2);
                ui.Text("Right", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));
            }
        }
    }

    private void BuildLayoutTab(UiFrame ui, UiContext ctx)
    {
        SectionHeader(ui, "Fixed vs Grow vs Fit");

        using (ui.Row("SizingRow").Gap(8).FitHeight().GrowWidth().Open())
        {
            using (ui.Panel("FixedBox")
                       .Width(120).Height(60)
                       .Background(UiColor.Rgb(60, 100, 80))
                       .CornerRadius(4)
                       .CenterChildren()
                       .Open())
            {
                ui.Text("Fixed 120x60", UiTextStyle.Default.WithSize(12));
            }

            using (ui.Panel("GrowBox")
                       .GrowWidth().Height(60)
                       .Background(UiColor.Rgb(80, 60, 100))
                       .CornerRadius(4)
                       .CenterChildren()
                       .Open())
            {
                ui.Text("Grow Width", UiTextStyle.Default.WithSize(12));
            }

            using (ui.Panel("FitBox")
                       .Fit()
                       .Background(UiColor.Rgb(100, 80, 60))
                       .CornerRadius(4)
                       .Padding(12, 8)
                       .Open())
            {
                ui.Text("Fit Content", UiTextStyle.Default.WithSize(12));
            }
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Child Alignment");

        using (ui.Row("AlignRow").Gap(8).FitHeight().GrowWidth().Open())
        {
            var alignments = new[] { ("TL", UiAlignX.Left, UiAlignY.Top), ("CC", UiAlignX.Center, UiAlignY.Center), ("BR", UiAlignX.Right, UiAlignY.Bottom) };

            for (var i = 0; i < alignments.Length; i++)
            {
                var (label, ax, ay) = alignments[i];
                using (ui.Panel($"AlignBox{i}")
                           .GrowWidth().Height(80)
                           .Background(UiColor.Rgb(40, 40, 50))
                           .Border(1, UiColor.Rgb(60, 60, 70))
                           .CornerRadius(4)
                           .Padding(8)
                           .AlignChildren(ax, ay)
                           .Open())
                {
                    ui.Text(label, UiTextStyle.Default.WithSize(14).WithColor(UiColor.Rgb(100, 200, 130)));
                }
            }
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Nested Layout");

        using (ui.Row("NestedOuter").Gap(8).FitHeight().GrowWidth().Open())
        {
            using (ui.Column("NestedLeft")
                       .Width(UiSizing.Percent(0.3f))
                       .FitHeight()
                       .Background(UiColor.Rgb(35, 35, 40))
                       .CornerRadius(4)
                       .Padding(12)
                       .Gap(8)
                       .Open())
            {
                ui.Text("Sidebar", UiTextStyle.Default.WithSize(14).WithColor(UiColor.Rgb(100, 200, 130)));
                Ui.Divider(ctx, UiColor.Rgb(60, 60, 65));

                for (var i = 0; i < 5; i++)
                {
                    using (ui.Panel($"SideItem{i}")
                               .GrowWidth().FitHeight()
                               .Background(UiColor.Rgb(45, 45, 50))
                               .CornerRadius(3)
                               .Padding(8, 6)
                               .Open())
                    {
                        ui.Text($"Item {i + 1}", UiTextStyle.Default.WithSize(13));
                    }
                }
            }

            using (ui.Column("NestedRight")
                       .GrowWidth()
                       .FitHeight()
                       .Background(UiColor.Rgb(35, 35, 40))
                       .CornerRadius(4)
                       .Padding(12)
                       .Gap(8)
                       .Open())
            {
                ui.Text("Main Content", UiTextStyle.Default.WithSize(14).WithColor(UiColor.Rgb(100, 149, 237)));
                Ui.Divider(ctx, UiColor.Rgb(60, 60, 65));

                for (var row = 0; row < 3; row++)
                {
                    using (ui.Row($"GridRow{row}").Gap(8).FitHeight().GrowWidth().Open())
                    {
                        for (var col = 0; col < 3; col++)
                        {
                            var hue = (byte)(60 + row * 30 + col * 20);
                            using (ui.Panel($"GridCell{row}_{col}")
                                       .GrowWidth().Height(50)
                                       .Background(UiColor.Rgb(hue, (byte)(hue / 2), (byte)(hue / 3)))
                                       .CornerRadius(4)
                                       .CenterChildren()
                                       .Open())
                            {
                                ui.Text($"R{row}C{col}", UiTextStyle.Default.WithSize(12));
                            }
                        }
                    }
                }
            }
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Scrollable Content");

        using (ui.Panel("ScrollDemo")
                   .GrowWidth().Height(150)
                   .Background(UiColor.Rgb(35, 35, 40))
                   .Border(1, UiColor.Rgb(60, 60, 65))
                   .CornerRadius(4)
                   .Padding(8)
                   .Gap(4)
                   .Vertical()
                   .ScrollVertical()
                   .Open())
        {
            for (var i = 0; i < 30; i++)
            {
                using (ui.Panel($"ScrollItem{i}")
                           .GrowWidth().FitHeight()
                           .Background(i % 2 == 0 ? UiColor.Rgb(42, 42, 47) : UiColor.Rgb(38, 38, 43))
                           .CornerRadius(3)
                           .Padding(8, 6)
                           .Open())
                {
                    ui.Text($"Scrollable item #{i + 1}", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));
                }
            }
        }
    }

    private void BuildStressTestTab(UiFrame ui, UiContext ctx)
    {
        SectionHeader(ui, "Many Elements");

        ui.Text("100 buttons rendered each frame:", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));
        Ui.VerticalSpacer(ctx, 4);

        using (ui.Panel("StressWrap")
                   .GrowWidth().FitHeight()
                   .Horizontal()
                   .Gap(4)
                   .Padding(4)
                   .Open())
        {
            for (var i = 0; i < 100; i++)
            {
                var r = (byte)(50 + i * 2 % 200);
                var g = (byte)(80 + i * 3 % 170);
                var b = (byte)(60 + i * 5 % 190);
                Ui.Button(ctx, $"SB{i}", $"{i}")
                    .Color(UiColor.Rgb(r, g, b))
                    .FontSize(11)
                    .Padding(6, 3)
                    .CornerRadius(3)
                    .Show();
            }
        }

        Ui.VerticalSpacer(ctx, 12);
        SectionHeader(ui, "Nested Depth");

        ui.Text("Deeply nested containers:", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));
        Ui.VerticalSpacer(ctx, 4);

        BuildNestedBoxes(ui, ctx, 0, 10);
    }

    private static void BuildNestedBoxes(UiFrame ui, UiContext ctx, int depth, int maxDepth)
    {
        if (depth >= maxDepth)
        {
            ui.Text("Innermost", UiTextStyle.Default.WithSize(12).WithColor(UiColor.Rgb(100, 200, 130)));
            return;
        }

        var brightness = (byte)(30 + depth * 8);
        var accent = (byte)(60 + depth * 15);

        using (ui.Panel($"Nest{depth}")
                   .GrowWidth().FitHeight()
                   .Background(UiColor.Rgb(brightness, brightness, (byte)(brightness + 5)))
                   .Border(1, UiColor.Rgb(accent, accent, (byte)(accent + 10)))
                   .CornerRadius(4)
                   .Padding(8)
                   .Gap(4)
                   .Vertical()
                   .Open())
        {
            ui.Text($"Depth {depth}", UiTextStyle.Default.WithSize(11).WithColor(UiColor.Rgb(accent, accent, (byte)(accent + 40))));
            BuildNestedBoxes(ui, ctx, depth + 1, maxDepth);
        }
    }

    private void BuildNewWidgetsTab(UiFrame ui, UiContext ctx)
    {
        SectionHeader(ui, "Sliders");

        using (ui.Column("SliderCol").Gap(8).FitHeight().GrowWidth().Open())
        {
            using (ui.Row("SliderRow1").Gap(8).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
            {
                ui.Text("Volume:", UiTextStyle.Default.WithSize(13));
                Ui.Slider(ctx, "Slider1")
                    .Range(0, 1)
                    .ShowValue(true, "P0")
                    .FillColor(UiColor.Rgb(64, 128, 96))
                    .Show(ref _sliderValue);
            }

            using (ui.Row("SliderRow2").Gap(8).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
            {
                ui.Text("Speed:", UiTextStyle.Default.WithSize(13));
                Ui.Slider(ctx, "Slider2")
                    .Range(0, 100)
                    .Step(5)
                    .ShowValue(true, "F0")
                    .FillColor(UiColor.Rgb(60, 130, 200))
                    .Show(ref _sliderValue2);
            }
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Draggable Value");

        using (ui.Row("DragRow").Gap(12).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
        {
            ui.Text("Scale:", UiTextStyle.Default.WithSize(13));
            Ui.DraggableValue(ctx, "DragVal1")
                .Label("S")
                .LabelColor(UiColor.Rgb(200, 150, 50))
                .Sensitivity(0.01f)
                .Format("F3")
                .Width(150)
                .Show(ref _dragValue);
            ui.Text($"= {_dragValue:F3}", UiTextStyle.Default.WithColor(UiColor.Gray).WithSize(12));
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Vector Editor");

        using (ui.Row("VecRow").Gap(8).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
        {
            ui.Text("Position:", UiTextStyle.Default.WithSize(13));
            Ui.Vec3Editor(ctx, "Vec3Pos", ref _posX, ref _posY, ref _posZ, 0.1f);
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Collapsible Sections");

        using (var cs1 = Ui.CollapsibleSection(ctx, "CS1", "Transform", true)
                   .HeaderBackground(UiColor.Rgb(45, 45, 50), UiColor.Rgb(55, 55, 60))
                   .Badge("3 props")
                   .Open())
        {
            if (cs1.IsExpanded)
            {
                using (ui.Row("CSRow1").Gap(8).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
                {
                    ui.Text("Position", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Gray));
                    Ui.Vec3Editor(ctx, "CSVec3", ref _posX, ref _posY, ref _posZ, 0.1f);
                }
                ui.Text("More transform properties would go here...", UiTextStyle.Default.WithSize(12).WithColor(UiColor.Rgb(140, 140, 140)));
            }
        }

        Ui.VerticalSpacer(ctx, 4);

        using (var cs2 = Ui.CollapsibleSection(ctx, "CS2", "Material", false)
                   .HeaderBackground(UiColor.Rgb(45, 45, 50), UiColor.Rgb(55, 55, 60))
                   .Badge("shader")
                   .Open())
        {
            if (cs2.IsExpanded)
            {
                ui.Text("Material settings collapsed by default.", UiTextStyle.Default.WithSize(12).WithColor(UiColor.Rgb(140, 140, 140)));
            }
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Tree View");

        using (ui.Row("TreeRow").Gap(12).FitHeight().GrowWidth().Open())
        {
            Ui.TreeView(ctx, "SceneTree", _treeNodes)
                .Width(250)
                .Height(200)
                .Show(ref _treeSelectedId);

            using (ui.Column("TreeInfo").Gap(4).FitHeight().GrowWidth().Open())
            {
                ui.Text("Selected:", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Gray));
                ui.Text(string.IsNullOrEmpty(_treeSelectedId) ? "(none)" : _treeSelectedId,
                    UiTextStyle.Default.WithSize(14).WithColor(UiColor.Rgb(100, 200, 130)));
            }
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Context Menu");

        using (ui.Row("CtxRow").Gap(8).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
        {
            var ctxBtn = Ui.Button(ctx, "CtxBtn", "Open Menu")
                .Color(UiColor.Rgb(60, 130, 200))
                .FontSize(13);
            if (ctxBtn.Show())
            {
                var state = Ui.GetContextMenuState(ctx, "CtxMenu");
                state.OpenBelow(ctxBtn.Id);
            }

            var clicked = Ui.ContextMenu(ctx, "CtxMenu", _contextMenuItems).Show();
            if (clicked >= 0)
            {
                ui.Text($"Clicked: {_contextMenuItems[clicked].Label}", UiTextStyle.Default.WithSize(12).WithColor(UiColor.Gray));
            }
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "Property Grid");

        using (var grid = Ui.PropertyGrid(ctx, "PropGrid").LabelWidth(100).Open())
        {
            using (grid.Row("Speed"))
            {
                Ui.DraggableValue(ctx, "PGSpeed")
                    .Label("V")
                    .LabelColor(UiColor.Rgb(60, 130, 200))
                    .Sensitivity(0.1f)
                    .Show(ref _propFloat);
            }

            using (grid.Row("Color"))
            {
                Ui.Vec3Editor(ctx, "PGColor", ref _propR, ref _propG, ref _propB, 0.01f);
            }

            using (grid.Row("Active"))
            {
                Ui.Checkbox(ctx, "PGCheck", "", _checkboxA).Show();
            }
        }

        Ui.VerticalSpacer(ctx, 12);

        SectionHeader(ui, "List Editor");

        var listResult = Ui.ListEditor(ctx, "ListEd")
            .Title("Components")
            .Height(180)
            .Show(_listItems.ToArray(), ref _listSelectedIndex);

        if (listResult.Added)
        {
            _listItems.Add($"Item {_listItems.Count + 1}");
        }

        if (listResult.Removed && listResult.RemovedIndex < _listItems.Count)
        {
            _listItems.RemoveAt(listResult.RemovedIndex);
            if (_listSelectedIndex >= _listItems.Count)
            {
                _listSelectedIndex = _listItems.Count - 1;
            }
        }
    }

    private static List<UiTreeNode> BuildSceneTree()
    {
        return
        [
            new UiTreeNode
            {
                Id = "root", Label = "Scene Root", Icon = FontAwesome.Cubes,
                Children =
                [
                    new UiTreeNode
                    {
                        Id = "camera", Label = "Main Camera", Icon = FontAwesome.Camera,
                        Children = []
                    },
                    new UiTreeNode
                    {
                        Id = "lights", Label = "Lights", Icon = FontAwesome.Lightbulb,
                        Children =
                        [
                            new UiTreeNode { Id = "dirLight", Label = "Directional Light", Icon = FontAwesome.Sun },
                            new UiTreeNode { Id = "pointLight", Label = "Point Light", Icon = FontAwesome.Circle }
                        ]
                    },
                    new UiTreeNode
                    {
                        Id = "objects", Label = "Objects", Icon = FontAwesome.Cube,
                        Children =
                        [
                            new UiTreeNode { Id = "player", Label = "Player", Icon = FontAwesome.User },
                            new UiTreeNode { Id = "terrain", Label = "Terrain", Icon = FontAwesome.Globe },
                            new UiTreeNode
                            {
                                Id = "props", Label = "Props", Icon = FontAwesome.ObjectGroup,
                                Children =
                                [
                                    new UiTreeNode { Id = "tree1", Label = "Tree", Icon = FontAwesome.Cube },
                                    new UiTreeNode { Id = "rock1", Label = "Rock", Icon = FontAwesome.Cube }
                                ]
                            }
                        ]
                    }
                ]
            }
        ];
    }

    private static void SectionHeader(UiFrame ui, string title)
    {
        ui.Text(title, new UiTextStyle
        {
            Color = UiColor.Rgb(100, 200, 130),
            FontSize = 16,
            Alignment = UiTextAlign.Left
        });
    }

    protected override void OnShutdown()
    {
        FontAwesome.Shutdown();
        _renderFrame?.Dispose();
    }
}
