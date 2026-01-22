using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using NiziKit.Components;
using NiziKit.Editor.Animation;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views.Editors;

public partial class AnimationPreviewEditor : UserControl
{
    private AnimationPreviewRenderer? _previewRenderer;
    private AnimatorComponent? _animatorComponent;
    private EditorViewModel? _editorViewModel;
    private bool _isDraggingTimeline;
    private SkiaTextureView? _textureView;

    public AnimationPreviewRenderer? PreviewRenderer => _previewRenderer;

    public AnimationPreviewEditor()
    {
        InitializeComponent();
    }

    public void SetAnimatorComponent(AnimatorComponent? animator, EditorViewModel? editorViewModel = null)
    {
        _animatorComponent = animator;
        _editorViewModel = editorViewModel;

        if (animator == null)
        {
            _editorViewModel?.UnregisterAnimationPreview(this);
            _previewRenderer?.Dispose();
            _previewRenderer = null;
            AnimationComboBox.ItemsSource = null;
            return;
        }

        _previewRenderer ??= new AnimationPreviewRenderer(256, 256);
        _previewRenderer.SetPreviewTarget(animator.Owner);

        var animations = _previewRenderer.GetAvailableAnimations();
        AnimationComboBox.ItemsSource = animations;

        if (animations.Count > 0)
        {
            AnimationComboBox.SelectedIndex = 0;
        }

        SetupPreviewView();
        UpdateDurationText();

        _editorViewModel?.RegisterAnimationPreview(this);
    }

    private void SetupPreviewView()
    {
        if (_textureView != null || _previewRenderer == null)
        {
            return;
        }

        _textureView = new SkiaTextureView
        {
            SourceTexture = _previewRenderer.ColorTarget
        };

        var previewBorder = this.FindControl<Border>("PreviewBorder");
        if (previewBorder?.Child is Grid grid)
        {
            grid.Children.Add(_textureView);
            PlaceholderText.IsVisible = false;
        }
    }

    public void Update(float deltaTime)
    {
        if (_previewRenderer == null)
        {
            return;
        }

        _previewRenderer.Update(deltaTime);

        if (!_isDraggingTimeline && _previewRenderer.IsPlaying)
        {
            TimelineSlider.Value = _previewRenderer.GetNormalizedTime();
            UpdateCurrentTimeText();
        }

        _textureView?.InvalidateVisual();
    }

    private void OnAnimationSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_previewRenderer == null)
        {
            return;
        }

        var selectedAnimation = AnimationComboBox.SelectedItem as string;
        if (string.IsNullOrEmpty(selectedAnimation))
        {
            return;
        }

        _previewRenderer.PlayAnimation(selectedAnimation);
        UpdateDurationText();
        UpdatePlayPauseButton();
    }

    private void OnTimelineValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isDraggingTimeline)
        {
            UpdateCurrentTimeText();
        }
    }

    private void OnPlayPauseClicked(object? sender, RoutedEventArgs e)
    {
        if (_previewRenderer == null)
        {
            return;
        }

        if (_previewRenderer.IsPlaying)
        {
            _previewRenderer.Pause();
        }
        else
        {
            if (_previewRenderer.CurrentAnimation == null && AnimationComboBox.SelectedItem is string anim)
            {
                _previewRenderer.PlayAnimation(anim);
            }
            else
            {
                _previewRenderer.Resume();
            }
        }

        UpdatePlayPauseButton();
    }

    private void OnStopClicked(object? sender, RoutedEventArgs e)
    {
        if (_previewRenderer == null)
        {
            return;
        }

        _previewRenderer.Stop();
        TimelineSlider.Value = 0;
        UpdatePlayPauseButton();
        UpdateCurrentTimeText();
    }

    private void OnSpeedValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_previewRenderer == null)
        {
            return;
        }

        _previewRenderer.PlaybackSpeed = (float)SpeedSlider.Value;
        SpeedText.Text = $"{SpeedSlider.Value:F1}x";
    }

    private void UpdatePlayPauseButton()
    {
        PlayPauseButton.Content = _previewRenderer?.IsPlaying == true ? "Pause" : "Play";
    }

    private void UpdateDurationText()
    {
        if (_previewRenderer == null)
        {
            return;
        }

        var duration = _previewRenderer.GetAnimationDuration();
        DurationText.Text = FormatTime(duration);
    }

    private void UpdateCurrentTimeText()
    {
        if (_previewRenderer == null)
        {
            return;
        }

        var duration = _previewRenderer.GetAnimationDuration();
        var currentTime = (float)TimelineSlider.Value * duration;
        CurrentTimeText.Text = FormatTime(currentTime);
    }

    private static string FormatTime(float seconds)
    {
        var mins = (int)(seconds / 60);
        var secs = seconds % 60;
        return $"{mins}:{secs:00.0}";
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _editorViewModel?.UnregisterAnimationPreview(this);
        _previewRenderer?.Dispose();
        _previewRenderer = null;
    }
}
