using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NiziKit.Animation;
using NiziKit.Components;
using NiziKit.Editor.Services;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views.Editors;

public partial class AnimationEntryEditor : UserControl
{
    private TextBox? _nameTextBox;
    private StackPanel? _sourcePanel;
    private Border? _externalBadge;
    private TextBox? _sourceTextBox;
    private Button? _browseButton;
    private bool _isUpdating;

    public AnimationEntry? Entry { get; set; }
    public AssetBrowserService? AssetBrowser { get; set; }
    public EditorViewModel? EditorViewModel { get; set; }
    public Action? OnValueChanged { get; set; }
    public bool IsReadOnly { get; set; }

    public AnimationEntryEditor()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _nameTextBox = this.FindControl<TextBox>("NameTextBox");
        _sourcePanel = this.FindControl<StackPanel>("SourcePanel");
        _externalBadge = this.FindControl<Border>("ExternalBadge");
        _sourceTextBox = this.FindControl<TextBox>("SourceTextBox");
        _browseButton = this.FindControl<Button>("BrowseButton");

        if (_nameTextBox != null)
        {
            _nameTextBox.LostFocus += OnNameLostFocus;
        }

        if (_browseButton != null)
        {
            _browseButton.Click += OnBrowseClicked;
        }
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

        _isUpdating = true;
        try
        {
            if (_nameTextBox != null)
            {
                _nameTextBox.Text = Entry.Name;
                _nameTextBox.IsReadOnly = IsReadOnly;
            }

            if (_sourcePanel != null)
            {
                _sourcePanel.IsVisible = Entry.IsExternal || !IsReadOnly;
            }

            if (_externalBadge != null)
            {
                _externalBadge.IsVisible = Entry.IsExternal;
            }

            if (_sourceTextBox != null)
            {
                _sourceTextBox.Text = Entry.SourceRef ?? "(From Skeleton)";
            }

            if (_browseButton != null)
            {
                _browseButton.IsEnabled = !IsReadOnly;
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void OnNameLostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isUpdating || Entry == null || IsReadOnly || _nameTextBox == null)
        {
            return;
        }

        var newName = _nameTextBox.Text ?? "";
        if (newName != Entry.Name)
        {
            Entry.Name = newName;
            OnValueChanged?.Invoke();
        }
    }

    private void OnBrowseClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Entry == null || IsReadOnly || EditorViewModel == null)
        {
            return;
        }

        string? currentPack = null;
        string? currentAssetName = null;

        if (!string.IsNullOrEmpty(Entry.SourceRef))
        {
            var colonIndex = Entry.SourceRef.IndexOf(':');
            if (colonIndex > 0)
            {
                currentPack = Entry.SourceRef[..colonIndex];
                currentAssetName = Entry.SourceRef[(colonIndex + 1)..];
            }
        }

        EditorViewModel.OpenAssetPicker(AssetRefType.Animation, currentPack, currentAssetName, asset =>
        {
            if (asset != null)
            {
                Entry.SourceRef = asset.FullReference;
                if (string.IsNullOrEmpty(Entry.Name))
                {
                    var slashIndex = asset.Name.IndexOf('/');
                    Entry.Name = slashIndex > 0 ? asset.Name[(slashIndex + 1)..] : asset.Name;
                }
                UpdateUI();
                OnValueChanged?.Invoke();
            }
        });
    }
}
