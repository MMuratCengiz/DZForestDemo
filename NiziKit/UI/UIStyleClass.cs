namespace NiziKit.UI;

public class UiStyleClass
{
    public UiColor? Background { get; set; }
    public UiBorder? Border { get; set; }
    public UiPadding? Padding { get; set; }
    public float? CornerRadius { get; set; }
    public UiSizing? Width { get; set; }
    public UiSizing? Height { get; set; }
    public UiDirection? Direction { get; set; }
    public float? Gap { get; set; }
    public UiAlignX? AlignX { get; set; }
    public UiAlignY? AlignY { get; set; }
    public bool? ScrollVertical { get; set; }
    public bool? ScrollHorizontal { get; set; }
}

public class UiTextStyleClass
{
    public UiColor? Color { get; set; }
    public ushort? FontSize { get; set; }
    public uint? FontId { get; set; }
    public UiTextWrap? WrapMode { get; set; }
    public UiTextAlign? Alignment { get; set; }
}
