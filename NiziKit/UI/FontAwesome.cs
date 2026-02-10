/*
Den Of Iz - Game/Game Engine
Copyright (c) 2020-2024 Muhammed Murat Cengiz

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using DenOfIz;

namespace NiziKit.UI;

public static class FontAwesome
{
    public const ushort FontId = 1;
    private const string EmbeddedResourceName = "NiziKit.UI.Fonts.FontAwesome.dzfont";

    private static Font? _font;
    private static FontAsset? _fontAsset;
    private static FontLibrary? _fontLibrary;
    private static GCHandle _fontDataHandle;
    private static bool _isInitialized;

    public const string AngleDown = "\uf107";
    public const string AngleLeft = "\uf104";
    public const string AngleRight = "\uf105";
    public const string AngleUp = "\uf106";
    public const string ArrowDown = "\uf063";
    public const string ArrowLeft = "\uf060";
    public const string ArrowRight = "\uf061";
    public const string ArrowUp = "\uf062";
    public const string ChevronDown = "\uf078";
    public const string ChevronLeft = "\uf053";
    public const string ChevronRight = "\uf054";
    public const string ChevronUp = "\uf077";
    public const string CaretDown = "\uf0d7";
    public const string CaretLeft = "\uf0d9";
    public const string CaretRight = "\uf0da";
    public const string CaretUp = "\uf0d8";

    public const string Check = "\uf00c";
    public const string Xmark = "\uf00d";
    public const string Plus = "\uf067";
    public const string Minus = "\uf068";
    public const string Times = "\uf00d";
    public const string Search = "\uf002";
    public const string Edit = "\uf044";
    public const string Trash = "\uf1f8";
    public const string TrashCan = "\uf2ed";
    public const string Copy = "\uf0c5";
    public const string Paste = "\uf0ea";
    public const string Cut = "\uf0c4";
    public const string Undo = "\uf0e2";
    public const string Redo = "\uf01e";
    public const string Save = "\uf0c7";
    public const string Download = "\uf019";
    public const string Upload = "\uf093";
    public const string Refresh = "\uf021";
    public const string Sync = "\uf021";

    public const string Bars = "\uf0c9";
    public const string EllipsisVertical = "\uf142";
    public const string EllipsisHorizontal = "\uf141";
    public const string Grip = "\uf58d";
    public const string GripVertical = "\uf58e";
    public const string Expand = "\uf065";
    public const string Compress = "\uf066";
    public const string Maximize = "\uf2d0";
    public const string Minimize = "\uf2d1";
    public const string WindowClose = "\uf410";
    public const string WindowMaximize = "\uf2d0";
    public const string WindowMinimize = "\uf2d1";
    public const string WindowRestore = "\uf2d2";

    public const string File = "\uf15b";
    public const string FileAlt = "\uf15c";
    public const string FileCode = "\uf1c9";
    public const string FileImage = "\uf1c5";
    public const string Folder = "\uf07b";
    public const string FolderOpen = "\uf07c";
    public const string FolderPlus = "\uf65e";
    public const string FolderMinus = "\uf65d";

    public const string Play = "\uf04b";
    public const string Pause = "\uf04c";
    public const string Stop = "\uf04d";
    public const string StepForward = "\uf051";
    public const string StepBackward = "\uf048";
    public const string FastForward = "\uf050";
    public const string FastBackward = "\uf049";
    public const string VolumeUp = "\uf028";
    public const string VolumeDown = "\uf027";
    public const string VolumeMute = "\uf6a9";
    public const string VolumeOff = "\uf026";

    public const string CircleCheck = "\uf058";
    public const string CircleXmark = "\uf057";
    public const string CircleInfo = "\uf05a";
    public const string CircleExclamation = "\uf06a";
    public const string CircleQuestion = "\uf059";
    public const string TriangleExclamation = "\uf071";
    public const string Bell = "\uf0f3";
    public const string BellSlash = "\uf1f6";
    public const string Flag = "\uf024";
    public const string Star = "\uf005";
    public const string StarHalf = "\uf089";
    public const string Heart = "\uf004";
    public const string Bookmark = "\uf02e";

    public const string Cog = "\uf013";
    public const string Gear = "\uf013";
    public const string Gears = "\uf085";
    public const string Wrench = "\uf0ad";
    public const string Screwdriver = "\uf54a";
    public const string Hammer = "\uf6e3";
    public const string Tools = "\uf7d9";
    public const string Lock = "\uf023";
    public const string LockOpen = "\uf3c1";
    public const string Key = "\uf084";
    public const string Home = "\uf015";
    public const string House = "\uf015";
    public const string User = "\uf007";
    public const string Users = "\uf0c0";
    public const string UserPlus = "\uf234";
    public const string UserMinus = "\uf503";
    public const string Globe = "\uf0ac";
    public const string Camera = "\uf030";
    public const string Image = "\uf03e";
    public const string Video = "\uf03d";
    public const string Music = "\uf001";
    public const string Microphone = "\uf130";
    public const string MicrophoneSlash = "\uf131";

    public const string Circle = "\uf111";
    public const string Square = "\uf0c8";
    public const string SquareFull = "\uf45c";

    public const string Bold = "\uf032";
    public const string Italic = "\uf033";
    public const string Underline = "\uf0cd";
    public const string Strikethrough = "\uf0cc";
    public const string AlignLeft = "\uf036";
    public const string AlignCenter = "\uf037";
    public const string AlignRight = "\uf038";
    public const string AlignJustify = "\uf039";
    public const string List = "\uf03a";
    public const string ListOl = "\uf0cb";
    public const string ListUl = "\uf0ca";
    public const string Indent = "\uf03c";
    public const string Outdent = "\uf03b";

    public const string Cube = "\uf1b2";
    public const string Cubes = "\uf1b3";
    public const string Gamepad = "\uf11b";
    public const string VrCardboard = "\uf729";
    public const string LayerGroup = "\uf5fd";
    public const string ObjectGroup = "\uf247";
    public const string ObjectUngroup = "\uf248";
    public const string VectorSquare = "\uf5cb";
    public const string DrawPolygon = "\uf5ee";
    public const string Bezier = "\uf55b";
    public const string Lightbulb = "\uf0eb";
    public const string Sun = "\uf185";
    public const string Moon = "\uf186";
    public const string Eye = "\uf06e";
    public const string EyeSlash = "\uf070";
    public const string Crosshairs = "\uf05b";
    public const string LocationCrosshairs = "\uf601";
    public const string Expand4 = "\uf31e";
    public const string Rotate = "\uf2f1";
    public const string RotateLeft = "\uf2ea";
    public const string RotateRight = "\uf2f9";
    public const string ArrowsUpDownLeftRight = "\uf047";
    public const string UpDownLeftRight = "\uf0b2";
    public const string Magnet = "\uf076";
    public const string Grid = "\ue011";
    public const string BorderAll = "\uf84c";
    public const string TableCells = "\uf00a";
    public const string ChartBar = "\uf080";
    public const string ChartLine = "\uf201";
    public const string Film = "\uf008";
    public const string MagnifyingGlass = "\uf002";
    public const string Bone = "\uf5d7";
    public const string ListCheck = "\uf0ae";

    public const string Info = "\uf129";
    public const string Question = "\uf128";
    public const string Exclamation = "\uf12a";
    public const string Bug = "\uf188";
    public const string Code = "\uf121";
    public const string Terminal = "\uf120";
    public const string Database = "\uf1c0";
    public const string Server = "\uf233";
    public const string Cloud = "\uf0c2";
    public const string Link = "\uf0c1";
    public const string Unlink = "\uf127";
    public const string ExternalLink = "\uf35d";
    public const string Share = "\uf064";
    public const string ShareNodes = "\uf1e0";
    public const string Rss = "\uf09e";
    public const string Wifi = "\uf1eb";
    public const string Signal = "\uf012";
    public const string Battery = "\uf240";
    public const string Plug = "\uf1e6";
    public const string Power = "\uf011";
    public const string Print = "\uf02f";
    public const string Keyboard = "\uf11c";
    public const string Mouse = "\uf8cc";
    public const string Desktop = "\uf108";
    public const string Laptop = "\uf109";
    public const string Mobile = "\uf3ce";
    public const string Tablet = "\uf3fa";
    public const string Clock = "\uf017";
    public const string Calendar = "\uf073";
    public const string CalendarDay = "\uf783";
    public const string Envelope = "\uf0e0";
    public const string PaperPlane = "\uf1d8";
    public const string Comment = "\uf075";
    public const string Comments = "\uf086";
    public const string Message = "\uf27a";
    public const string Phone = "\uf095";
    public const string Tag = "\uf02b";
    public const string Tags = "\uf02c";
    public const string Filter = "\uf0b0";
    public const string Sort = "\uf0dc";
    public const string SortUp = "\uf0de";
    public const string SortDown = "\uf0dd";
    public const string Sliders = "\uf1de";
    public const string Palette = "\uf53f";
    public const string Brush = "\uf55d";
    public const string PaintBrush = "\uf1fc";
    public const string Eraser = "\uf12d";
    public const string FillDrip = "\uf576";
    public const string EyeDropper = "\uf1fb";
    public const string Ruler = "\uf545";
    public const string RulerCombined = "\uf546";
    public const string Compass = "\uf14e";
    public const string Sitemap = "\uf0e8";
    public const string DiagramProject = "\uf542";
    public const string PuzzlePiece = "\uf12e";

    public static void Initialize(Clay clay, string fontPath, ushort fontId = FontId)
    {
        if (_isInitialized)
        {
            return;
        }

        _fontLibrary = new FontLibrary();
        _font = _fontLibrary.LoadFontFromPath(StringView.Create(fontPath));
        clay.AddFont(fontId, _font);
        _isInitialized = true;
    }

    public static void InitializeWithImport(
        Clay clay,
        string fontPath,
        string outputDirectory,
        ushort fontId = FontId,
        uint fontSize = 32)
    {
        if (_isInitialized)
        {
            return;
        }

        using var importer = new FontImporter();

        var ranges = new UnicodeRange[]
        {
            new() { Start = 0xE000, End = 0xE0FF },
            new() { Start = 0xF000, End = 0xF8FF }
        };

        var rangesHandle = GCHandle.Alloc(ranges, GCHandleType.Pinned);
        try
        {
            var importDesc = new FontImportDesc
            {
                SourceFilePath = StringView.Create(fontPath),
                TargetDirectory = StringView.Create(outputDirectory),
                AssetNamePrefix = StringView.Create("fa"),
                InitialFontSize = fontSize,
                AtlasWidth = 0,
                AtlasHeight = 0,
                CustomRanges = new UnicodeRangeArray
                {
                    Elements = rangesHandle.AddrOfPinnedObject(),
                    NumElements = (ulong)ranges.Length
                }
            };

            var result = importer.Import(importDesc);
            if (result.ResultCode != ImporterResultCode.Success)
            {
                throw new InvalidOperationException(
                    $"Failed to import FontAwesome: {result.ErrorMessage}");
            }

            _fontLibrary = new FontLibrary();
            _font = _fontLibrary.LoadFontFromPath(StringView.Create(fontPath));
            clay.AddFont(fontId, _font);
            _isInitialized = true;
        }
        finally
        {
            rangesHandle.Free();
        }
    }

    public static void InitializeFromAsset(Clay clay, FontAsset fontAsset, ushort fontId = FontId)
    {
        if (_isInitialized)
        {
            return;
        }

        _fontLibrary = new FontLibrary();
        var fontDesc = new FontDesc { FontAsset = fontAsset };
        _font = _fontLibrary.LoadFontFromDesc(fontDesc);
        clay.AddFont(fontId, _font);
        _isInitialized = true;
    }

    public static void InitializeEmbedded(Clay clay, ushort fontId = FontId)
    {
        if (_isInitialized)
        {
            return;
        }

        var assembly = typeof(FontAwesome).Assembly;
        using var stream = assembly.GetManifestResourceStream(EmbeddedResourceName);

        if (stream == null)
        {
            var available = string.Join(", ", assembly.GetManifestResourceNames());
            throw new InvalidOperationException(
                $"FontAwesome embedded resource '{EmbeddedResourceName}' not found. " +
                $"Available resources: {available}. " +
                "Run 'dotnet run --project NiziKit.Build -- import-font <font.otf> NiziKit/UI/Fonts' first.");
        }

        var fontData = new byte[stream.Length];
        _ = stream.Read(fontData, 0, fontData.Length);

        _fontDataHandle = GCHandle.Alloc(fontData, GCHandleType.Pinned);

        var dataView = new ByteArrayView
        {
            Elements = _fontDataHandle.AddrOfPinnedObject(),
            NumElements = (ulong)fontData.Length
        };

        using var binaryReader = DenOfIz.BinaryReader.CreateFromData(dataView, new BinaryReaderDesc { NumBytes = 0 });

        var fontAssetReaderDesc = new FontAssetReaderDesc { Reader = binaryReader };
        using var fontAssetReader = new FontAssetReader(fontAssetReaderDesc);

        _fontAsset = fontAssetReader.Read();
        _fontLibrary = new FontLibrary();

        var fontDesc = new FontDesc { FontAsset = _fontAsset };
        _font = _fontLibrary.LoadFontFromDesc(fontDesc);
        clay.AddFont(fontId, _font);
        _isInitialized = true;
    }

    public static void InitializeFromDzFont(Clay clay, string dzfontPath, ushort fontId = FontId)
    {
        if (_isInitialized)
        {
            return;
        }

        var fontData = System.IO.File.ReadAllBytes(dzfontPath);

        _fontDataHandle = GCHandle.Alloc(fontData, GCHandleType.Pinned);

        var dataView = new ByteArrayView
        {
            Elements = _fontDataHandle.AddrOfPinnedObject(),
            NumElements = (ulong)fontData.Length
        };

        using var binaryReader = DenOfIz.BinaryReader.CreateFromData(dataView, new BinaryReaderDesc { NumBytes = 0 });

        var fontAssetReaderDesc = new FontAssetReaderDesc { Reader = binaryReader };
        using var fontAssetReader = new FontAssetReader(fontAssetReaderDesc);

        _fontAsset = fontAssetReader.Read();
        _fontLibrary = new FontLibrary();

        var fontDesc = new FontDesc { FontAsset = _fontAsset };
        _font = _fontLibrary.LoadFontFromDesc(fontDesc);
        clay.AddFont(fontId, _font);
        _isInitialized = true;
    }

    public static bool IsInitialized => _isInitialized;

    public static Font? Font => _font;

    public static UiTextStyle Style(UiColor color, ushort size = 14)
    {
        return new UiTextStyle
        {
            Color = color,
            FontSize = size,
            FontId = FontId,
            Alignment = UiTextAlign.Center
        };
    }

    public static void Shutdown()
    {
        _font?.Dispose();
        _fontAsset?.Dispose();
        _fontLibrary?.Dispose();

        if (_fontDataHandle.IsAllocated)
        {
            _fontDataHandle.Free();
        }

        _font = null;
        _fontAsset = null;
        _fontLibrary = null;
        _isInitialized = false;
    }
}
