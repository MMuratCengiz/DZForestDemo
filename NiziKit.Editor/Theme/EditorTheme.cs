namespace NiziKit.Editor.Theme;

public static class EditorTheme
{
    private static IEditorTheme _current = new DarkEditorTheme();

    public static IEditorTheme Current
    {
        get => _current;
        set => _current = value ?? new DarkEditorTheme();
    }

    public static void UseDark() => _current = new DarkEditorTheme();
    public static void UseLight() => _current = new LightEditorTheme();
}
