using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NiziKit.Animation;

namespace NiziKit.Editor.Views.Editors;

public partial class AnimationEntryEditor : UserControl
{
    private Border? _externalBadge;
    private TextBlock? _animationNameText;

    public AnimationEntry? Entry { get; set; }

    public AnimationEntryEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _externalBadge = this.FindControl<Border>("ExternalBadge");
        _animationNameText = this.FindControl<TextBlock>("AnimationNameText");
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (Entry == null)
        {
            return;
        }

        if (_externalBadge != null)
        {
            _externalBadge.IsVisible = Entry.IsExternal;
        }

        if (_animationNameText != null)
        {
            _animationNameText.Text = Entry.IsExternal 
                ? $"{Entry.Name} ({Entry.SourceRef})" 
                : Entry.Name;
        }
    }
}
