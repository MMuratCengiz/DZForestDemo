using DenOfIz;
using NiziKit.Application;
using NiziKit.Application.Timing;
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
    private readonly List<string> _listItems = ["Item Alpha", "Item Beta", "Item Gamma", "Item Delta"];

    public override Type RendererType => typeof(ForwardRenderer);

    protected override void Load(Game game)
    {
        _renderFrame = new RenderFrame();
        _renderFrame.EnableDebugOverlay(DebugOverlayConfig.Default);
        _renderFrame.EnableUi(UiContextDesc.Default);
    }

    protected override void OnEvent(ref Event ev)
    {
    }

    protected override void Update(float dt)
    {
        _renderFrame.BeginFrame();

        var debugOverlay = _renderFrame.RenderDebugOverlay();
        var ui = _renderFrame.RenderUi(BuildUi);

        _renderFrame.AlphaBlit(ui, debugOverlay);

        _renderFrame.Submit();
        _renderFrame.Present(debugOverlay);

    }

    private void BuildUi()
    {
        using var root = NiziUi.Root()
            .Background(UiColor.Rgb(30, 30, 34))
            .Vertical()
            .Padding(0)
            .Gap(0)
            .Open();

        using (NiziUi.Panel("TitleBar")
                   .Horizontal()
                   .GrowWidth()
                   .FitHeight()
                   .Background(UiColor.Rgb(20, 20, 24))
                   .Padding(16, 10)
                   .AlignChildren(UiAlignX.Left, UiAlignY.Center)
                   .Gap(12)
                   .Open())
        {
            NiziUi.Icon(FontAwesome.Cubes, UiColor.Rgb(100, 200, 130), 20);

            NiziUi.Text("NiziKit UI Test", new UiTextStyle
            {
                Color = UiColor.Rgb(100, 200, 130),
                FontSize = 20,
                Alignment = UiTextAlign.Left
            });

            NiziUi.FlexSpacer();

            NiziUi.Icon(FontAwesome.Clock, UiColor.Rgb(180, 180, 180), 14);
            NiziUi.Text($"FPS: {Time.FramesPerSecond:F0}", new UiTextStyle
            {
                Color = UiColor.Rgb(180, 180, 180),
                FontSize = 14,
            });

            NiziUi.Icon(FontAwesome.Gear, UiColor.Rgb(150, 150, 150), 16);
        }

        using (NiziUi.Panel("TabBar")
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
                var tabBtn = NiziUi.Button($"Tab{i}", _tabs[i])
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

        using (NiziUi.Panel("Content")
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
                    BuildWidgetsTab();
                    break;
                case 1:
                    BuildLayoutTab();
                    break;
                case 2:
                    BuildStressTestTab();
                    break;
                case 3:
                    BuildNewWidgetsTab();
                    break;
            }
        }
    }

    private void BuildWidgetsTab()
    {
        SectionHeader("FontAwesome Icons");

        using (NiziUi.Row("IconsRow").Gap(16).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
        {
            NiziUi.Icon(FontAwesome.Home, UiColor.Rgb(100, 200, 130), 20);
            NiziUi.Icon(FontAwesome.User, UiColor.Rgb(100, 149, 237), 20);
            NiziUi.Icon(FontAwesome.Gear, UiColor.Rgb(200, 150, 100), 20);
            NiziUi.Icon(FontAwesome.Search, UiColor.Rgb(180, 100, 180), 20);
            NiziUi.Icon(FontAwesome.Bell, UiColor.Rgb(200, 200, 100), 20);
            NiziUi.Icon(FontAwesome.Heart, UiColor.Rgb(220, 80, 80), 20);
            NiziUi.Icon(FontAwesome.Star, UiColor.Rgb(255, 200, 50), 20);
            NiziUi.Icon(FontAwesome.Check, UiColor.Rgb(100, 200, 100), 20);
            NiziUi.Icon(FontAwesome.Xmark, UiColor.Rgb(200, 80, 80), 20);
            NiziUi.Icon(FontAwesome.Plus, UiColor.Rgb(100, 180, 220), 20);
            NiziUi.Icon(FontAwesome.Folder, UiColor.Rgb(220, 180, 80), 20);
            NiziUi.Icon(FontAwesome.File, UiColor.Rgb(180, 180, 180), 20);
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Buttons");

        using (NiziUi.Row("ButtonRow").Gap(8).FitHeight().GrowWidth().Open())
        {
            if (NiziUi.Button("BtnDefault", "Default").Show())
            {
                _clickCount++;
            }

            if (NiziUi.Button("BtnPrimary", "Primary")
                .Color(UiColor.Rgb(60, 130, 200))
                .Show())
            {
                _clickCount++;
            }

            if (NiziUi.Button("BtnDanger", "Danger")
                .Color(UiColor.Rgb(200, 60, 60))
                .Show())
            {
                _clickCount++;
            }

            NiziUi.Button("BtnGhost", "Ghost")
                .Color(UiColor.Transparent, UiColor.Rgba(255, 255, 255, 20), UiColor.Rgba(255, 255, 255, 10))
                .Border(1, UiColor.Rgb(70, 70, 75))
                .Show();

            NiziUi.HorizontalSpacer(16);

            NiziUi.Text($"Clicks: {_clickCount}", UiTextStyle.Default.WithColor(UiColor.Rgb(180, 180, 180)));
        }

        NiziUi.VerticalSpacer(8);

        using (NiziUi.Row("CustomBtnRow").Gap(8).FitHeight().GrowWidth().Open())
        {
            if (NiziUi.Button("BtnLarge", "Large Button")
                    .Color(UiColor.Rgb(64, 128, 96))
                    .FontSize(18)
                    .Padding(24, 12)
                    .CornerRadius(8)
                    .Show())
            {
                _clickCount++;
            }

            if (NiziUi.Button("BtnSmall", "Small")
                    .FontSize(11)
                    .Padding(8, 4)
                    .CornerRadius(3)
                    .Show())
            {
                _clickCount++;
            }
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Checkboxes");

        using (NiziUi.Row("CheckboxRow").Gap(16).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
        {
            _checkboxA = NiziUi.Checkbox("ChkA", "Enable feature A", _checkboxA)
                .FontSize(14)
                .BoxSize(18)
                .CheckColor(UiColor.Rgb(64, 128, 96))
                .Show();

            _checkboxB = NiziUi.Checkbox("ChkB", "Enable feature B", _checkboxB)
                .FontSize(14)
                .BoxSize(18)
                .CheckColor(UiColor.Rgb(60, 130, 200))
                .Show();

            NiziUi.Text($"A={_checkboxA}, B={_checkboxB}", UiTextStyle.Default.WithColor(UiColor.Gray).WithSize(12));
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Text Fields");

        using (NiziUi.Column("TextFieldCol").Gap(8).FitHeight().GrowWidth().Open())
        {
            using (NiziUi.Row("TFRow1").Gap(8).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
            {
                NiziUi.Text("Single line:", UiTextStyle.Default.WithSize(14));
                NiziUi.TextField("TF1", ref _textFieldValue)
                    .Width(UiSizing.Grow())
                    .FontSize(14)
                    .Placeholder("Type here...")
                    .Show(Time.DeltaTime);
            }

            NiziUi.Text("Multi-line:", UiTextStyle.Default.WithSize(14));
            NiziUi.TextField("TFMulti", ref _multilineText)
                .Width(UiSizing.Grow())
                .Height(100)
                .FontSize(14)
                .Multiline()
                .Placeholder("Multi-line input...")
                .Show(Time.DeltaTime);
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Dropdown");

        using (NiziUi.Row("DropdownRow").Gap(8).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
        {
            NiziUi.Text("Selection:", UiTextStyle.Default.WithSize(14));
            NiziUi.Dropdown("DD1", _dropdownItems)
                .Width(200)
                .FontSize(14)
                .Show(ref _dropdownIndex);

            NiziUi.Text($"Selected: {_dropdownItems[Math.Max(0, _dropdownIndex)]}", UiTextStyle.Default.WithColor(UiColor.Gray).WithSize(12));
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Cards");

        using (NiziUi.Row("CardRow").Gap(12).FitHeight().GrowWidth().Open())
        {
            using (NiziUi.Card("Card1")
                       .Background(UiColor.Rgb(40, 40, 45))
                       .Border(1, UiColor.Rgb(60, 60, 65))
                       .Padding(16)
                       .Gap(8)
                       .GrowWidth()
                       .Open())
            {
                NiziUi.Text("Card Title", UiTextStyle.Default.WithSize(16).WithColor(UiColor.Rgb(100, 200, 130)));
                NiziUi.Text("This is a card component with background, border, padding, and gap.", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));
                NiziUi.Divider(UiColor.Rgb(60, 60, 65));
                NiziUi.Text("Footer content", UiTextStyle.Default.WithSize(12).WithColor(UiColor.Gray));
            }

            using (NiziUi.Card("Card2")
                       .Background(UiColor.Rgb(40, 40, 45))
                       .Border(1, UiColor.Rgb(60, 60, 65))
                       .Padding(16)
                       .Gap(8)
                       .GrowWidth()
                       .Open())
            {
                NiziUi.Text("Another Card", UiTextStyle.Default.WithSize(16).WithColor(UiColor.Rgb(100, 149, 237)));
                NiziUi.Text("Cards can hold any combination of widgets and layout elements.", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));

                if (NiziUi.Button("CardBtn", "Card Action")
                        .Color(UiColor.Rgb(60, 130, 200))
                        .FontSize(13)
                        .GrowWidth()
                        .Show())
                {
                    _clickCount++;
                }
            }
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Dividers & Spacers");

        using (NiziUi.Column("DividerCol").Gap(8).FitHeight().GrowWidth().Open())
        {
            NiziUi.Text("Above divider", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));
            NiziUi.Divider(UiColor.Rgb(80, 80, 85), 2);
            NiziUi.Text("Below divider", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));

            using (NiziUi.Row("VDivRow").Gap(8).FitHeight(0, 40).GrowWidth().Open())
            {
                NiziUi.Text("Left", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));
                NiziUi.VerticalDivider(UiColor.Rgb(80, 80, 85), 2);
                NiziUi.Text("Right", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));
            }
        }
    }

    private void BuildLayoutTab()
    {
        SectionHeader("Fixed vs Grow vs Fit");

        using (NiziUi.Row("SizingRow").Gap(8).FitHeight().GrowWidth().Open())
        {
            using (NiziUi.Panel("FixedBox")
                       .Width(120).Height(60)
                       .Background(UiColor.Rgb(60, 100, 80))
                       .CornerRadius(4)
                       .CenterChildren()
                       .Open())
            {
                NiziUi.Text("Fixed 120x60", UiTextStyle.Default.WithSize(12));
            }

            using (NiziUi.Panel("GrowBox")
                       .GrowWidth().Height(60)
                       .Background(UiColor.Rgb(80, 60, 100))
                       .CornerRadius(4)
                       .CenterChildren()
                       .Open())
            {
                NiziUi.Text("Grow Width", UiTextStyle.Default.WithSize(12));
            }

            using (NiziUi.Panel("FitBox")
                       .Fit()
                       .Background(UiColor.Rgb(100, 80, 60))
                       .CornerRadius(4)
                       .Padding(12, 8)
                       .Open())
            {
                NiziUi.Text("Fit Content", UiTextStyle.Default.WithSize(12));
            }
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Child Alignment");

        using (NiziUi.Row("AlignRow").Gap(8).FitHeight().GrowWidth().Open())
        {
            var alignments = new[] { ("TL", UiAlignX.Left, UiAlignY.Top), ("CC", UiAlignX.Center, UiAlignY.Center), ("BR", UiAlignX.Right, UiAlignY.Bottom) };

            for (var i = 0; i < alignments.Length; i++)
            {
                var (label, ax, ay) = alignments[i];
                using (NiziUi.Panel($"AlignBox{i}")
                           .GrowWidth().Height(80)
                           .Background(UiColor.Rgb(40, 40, 50))
                           .Border(1, UiColor.Rgb(60, 60, 70))
                           .CornerRadius(4)
                           .Padding(8)
                           .AlignChildren(ax, ay)
                           .Open())
                {
                    NiziUi.Text(label, UiTextStyle.Default.WithSize(14).WithColor(UiColor.Rgb(100, 200, 130)));
                }
            }
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Nested Layout");

        using (NiziUi.Row("NestedOuter").Gap(8).FitHeight().GrowWidth().Open())
        {
            using (NiziUi.Column("NestedLeft")
                       .Width(UiSizing.Percent(0.3f))
                       .FitHeight()
                       .Background(UiColor.Rgb(35, 35, 40))
                       .CornerRadius(4)
                       .Padding(12)
                       .Gap(8)
                       .Open())
            {
                NiziUi.Text("Sidebar", UiTextStyle.Default.WithSize(14).WithColor(UiColor.Rgb(100, 200, 130)));
                NiziUi.Divider(UiColor.Rgb(60, 60, 65));

                for (var i = 0; i < 5; i++)
                {
                    using (NiziUi.Panel($"SideItem{i}")
                               .GrowWidth().FitHeight()
                               .Background(UiColor.Rgb(45, 45, 50))
                               .CornerRadius(3)
                               .Padding(8, 6)
                               .Open())
                    {
                        NiziUi.Text($"Item {i + 1}", UiTextStyle.Default.WithSize(13));
                    }
                }
            }

            using (NiziUi.Column("NestedRight")
                       .GrowWidth()
                       .FitHeight()
                       .Background(UiColor.Rgb(35, 35, 40))
                       .CornerRadius(4)
                       .Padding(12)
                       .Gap(8)
                       .Open())
            {
                NiziUi.Text("Main Content", UiTextStyle.Default.WithSize(14).WithColor(UiColor.Rgb(100, 149, 237)));
                NiziUi.Divider(UiColor.Rgb(60, 60, 65));

                for (var row = 0; row < 3; row++)
                {
                    using (NiziUi.Row($"GridRow{row}").Gap(8).FitHeight().GrowWidth().Open())
                    {
                        for (var col = 0; col < 3; col++)
                        {
                            var hue = (byte)(60 + row * 30 + col * 20);
                            using (NiziUi.Panel($"GridCell{row}_{col}")
                                       .GrowWidth().Height(50)
                                       .Background(UiColor.Rgb(hue, (byte)(hue / 2), (byte)(hue / 3)))
                                       .CornerRadius(4)
                                       .CenterChildren()
                                       .Open())
                            {
                                NiziUi.Text($"R{row}C{col}", UiTextStyle.Default.WithSize(12));
                            }
                        }
                    }
                }
            }
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Scrollable Content");

        using (NiziUi.Panel("ScrollDemo")
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
                using (NiziUi.Panel($"ScrollItem{i}")
                           .GrowWidth().FitHeight()
                           .Background(i % 2 == 0 ? UiColor.Rgb(42, 42, 47) : UiColor.Rgb(38, 38, 43))
                           .CornerRadius(3)
                           .Padding(8, 6)
                           .Open())
                {
                    NiziUi.Text($"Scrollable item #{i + 1}", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));
                }
            }
        }
    }

    private void BuildStressTestTab()
    {
        SectionHeader("Many Elements");

        NiziUi.Text("100 buttons rendered each frame:", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));
        NiziUi.VerticalSpacer(4);

        using (NiziUi.Panel("StressWrap")
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
                NiziUi.Button($"SB{i}", $"{i}")
                    .Color(UiColor.Rgb(r, g, b))
                    .FontSize(11)
                    .Padding(6, 3)
                    .CornerRadius(3)
                    .Show();
            }
        }

        NiziUi.VerticalSpacer(12);
        SectionHeader("Nested Depth");

        NiziUi.Text("Deeply nested containers:", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Rgb(180, 180, 180)));
        NiziUi.VerticalSpacer(4);

        BuildNestedBoxes(0, 10);
    }

    private static void BuildNestedBoxes(int depth, int maxDepth)
    {
        if (depth >= maxDepth)
        {
            NiziUi.Text("Innermost", UiTextStyle.Default.WithSize(12).WithColor(UiColor.Rgb(100, 200, 130)));
            return;
        }

        var brightness = (byte)(30 + depth * 8);
        var accent = (byte)(60 + depth * 15);

        using (NiziUi.Panel($"Nest{depth}")
                   .GrowWidth().FitHeight()
                   .Background(UiColor.Rgb(brightness, brightness, (byte)(brightness + 5)))
                   .Border(1, UiColor.Rgb(accent, accent, (byte)(accent + 10)))
                   .CornerRadius(4)
                   .Padding(8)
                   .Gap(4)
                   .Vertical()
                   .Open())
        {
            NiziUi.Text($"Depth {depth}", UiTextStyle.Default.WithSize(11).WithColor(UiColor.Rgb(accent, accent, (byte)(accent + 40))));
            BuildNestedBoxes(depth + 1, maxDepth);
        }
    }

    private void BuildNewWidgetsTab()
    {
        SectionHeader("Sliders");

        using (NiziUi.Column("SliderCol").Gap(8).FitHeight().GrowWidth().Open())
        {
            using (NiziUi.Row("SliderRow1").Gap(8).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
            {
                NiziUi.Text("Volume:", UiTextStyle.Default.WithSize(13));
                NiziUi.Slider("Slider1")
                    .Range(0, 1)
                    .ShowValue(true, "P0")
                    .FillColor(UiColor.Rgb(64, 128, 96))
                    .Show(ref _sliderValue);
            }

            using (NiziUi.Row("SliderRow2").Gap(8).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
            {
                NiziUi.Text("Speed:", UiTextStyle.Default.WithSize(13));
                NiziUi.Slider("Slider2")
                    .Range(0, 100)
                    .Step(5)
                    .ShowValue(true, "F0")
                    .FillColor(UiColor.Rgb(60, 130, 200))
                    .Show(ref _sliderValue2);
            }
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Draggable Value");

        using (NiziUi.Row("DragRow").Gap(12).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
        {
            NiziUi.Text("Scale:", UiTextStyle.Default.WithSize(13));
            NiziUi.DraggableValue("DragVal1")
                .Label("S")
                .LabelColor(UiColor.Rgb(200, 150, 50))
                .Sensitivity(0.01f)
                .Format("F3")
                .Width(150)
                .Show(ref _dragValue);
            NiziUi.Text($"= {_dragValue:F3}", UiTextStyle.Default.WithColor(UiColor.Gray).WithSize(12));
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Vector Editor");

        using (NiziUi.Row("VecRow").Gap(8).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
        {
            NiziUi.Text("Position:", UiTextStyle.Default.WithSize(13));
            NiziUi.Vec3Editor("Vec3Pos", ref _posX, ref _posY, ref _posZ, 0.1f);
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Collapsible Sections");

        using (var cs1 = NiziUi.CollapsibleSection("CS1", "Transform", true)
                   .HeaderBackground(UiColor.Rgb(45, 45, 50), UiColor.Rgb(55, 55, 60))
                   .Badge("3 props")
                   .Open())
        {
            if (cs1.IsExpanded)
            {
                using (NiziUi.Row("CSRow1").Gap(8).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
                {
                    NiziUi.Text("Position", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Gray));
                    NiziUi.Vec3Editor("CSVec3", ref _posX, ref _posY, ref _posZ, 0.1f);
                }
                NiziUi.Text("More transform properties would go here...", UiTextStyle.Default.WithSize(12).WithColor(UiColor.Rgb(140, 140, 140)));
            }
        }

        NiziUi.VerticalSpacer(4);

        using (var cs2 = NiziUi.CollapsibleSection("CS2", "Material", false)
                   .HeaderBackground(UiColor.Rgb(45, 45, 50), UiColor.Rgb(55, 55, 60))
                   .Badge("shader")
                   .Open())
        {
            if (cs2.IsExpanded)
            {
                NiziUi.Text("Material settings collapsed by default.", UiTextStyle.Default.WithSize(12).WithColor(UiColor.Rgb(140, 140, 140)));
            }
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Tree View");

        using (NiziUi.Row("TreeRow").Gap(12).FitHeight().GrowWidth().Open())
        {
            NiziUi.TreeView("SceneTree", _treeNodes)
                .Width(250)
                .Height(200)
                .Show(ref _treeSelectedId);

            using (NiziUi.Column("TreeInfo").Gap(4).FitHeight().GrowWidth().Open())
            {
                NiziUi.Text("Selected:", UiTextStyle.Default.WithSize(13).WithColor(UiColor.Gray));
                NiziUi.Text(string.IsNullOrEmpty(_treeSelectedId) ? "(none)" : _treeSelectedId,
                    UiTextStyle.Default.WithSize(14).WithColor(UiColor.Rgb(100, 200, 130)));
            }
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Context Menu");

        using (NiziUi.Row("CtxRow").Gap(8).FitHeight().GrowWidth().AlignChildren(UiAlignX.Left, UiAlignY.Center).Open())
        {
            var ctxBtn = NiziUi.Button("CtxBtn", "Open Menu")
                .Color(UiColor.Rgb(60, 130, 200))
                .FontSize(13);
            if (ctxBtn.Show())
            {
                var state = NiziUi.GetContextMenuState("CtxMenu");
                state.OpenBelow(ctxBtn.Id);
            }

            var clicked = NiziUi.ContextMenu("CtxMenu", _contextMenuItems).Show();
            if (clicked >= 0)
            {
                NiziUi.Text($"Clicked: {_contextMenuItems[clicked].Label}", UiTextStyle.Default.WithSize(12).WithColor(UiColor.Gray));
            }
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("Property Grid");

        using (var grid = NiziUi.PropertyGrid("PropGrid").LabelWidth(100).Open())
        {
            using (grid.Row("Speed"))
            {
                NiziUi.DraggableValue("PGSpeed")
                    .Label("V")
                    .LabelColor(UiColor.Rgb(60, 130, 200))
                    .Sensitivity(0.1f)
                    .Show(ref _propFloat);
            }

            using (grid.Row("Color"))
            {
                NiziUi.Vec3Editor("PGColor", ref _propR, ref _propG, ref _propB, 0.01f);
            }

            using (grid.Row("Active"))
            {
                NiziUi.Checkbox("PGCheck", "", _checkboxA).Show();
            }
        }

        NiziUi.VerticalSpacer(12);

        SectionHeader("List Editor");

        var listResult = NiziUi.ListEditor("ListEd")
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

    private static void SectionHeader(string title)
    {
        NiziUi.Text(title, new UiTextStyle
        {
            Color = UiColor.Rgb(100, 200, 130),
            FontSize = 16,
            Alignment = UiTextAlign.Left
        });
    }

    protected override void OnShutdown()
    {
        _renderFrame?.Dispose();
    }
}
