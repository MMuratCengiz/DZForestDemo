using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using NiziKit.Animation;
using NiziKit.Editor.ViewModels;

namespace NiziKit.Editor.Views.Editors;

public partial class AnimationPreviewEditor : UserControl
{
    private Animator? _animator;
    private EditorViewModel? _editorViewModel;

    public AnimationPreviewEditor()
    {
        InitializeComponent();
    }

    public void SetAnimator(Animator? animator, EditorViewModel? editorViewModel = null)
    {
        _animator = animator;
        _editorViewModel = editorViewModel;

        if (animator == null)
        {
            _editorViewModel?.UnregisterAnimationPreview(this);
            AnimationComboBox.ItemsSource = null;
            return;
        }

        if (!animator.IsInitialized)
        {
            animator.Initialize();
        }

        var animations = animator.AnimationNames;
        AnimationComboBox.ItemsSource = animations;

        if (animations.Count > 0)
        {
            var defaultAnim = animator.DefaultAnimation;
            var selectedIndex = 0;

            if (!string.IsNullOrEmpty(defaultAnim))
            {
                for (var i = 0; i < animations.Count; i++)
                {
                    if (animations[i] == defaultAnim)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            AnimationComboBox.SelectedIndex = selectedIndex;
        }

        UpdateDurationText();
        _editorViewModel?.RegisterAnimationPreview(this);
    }

    public void Update(float deltaTime)
    {
        if (_animator == null)
        {
            return;
        }

        UpdateTimelineFromAnimator();
        UpdatePlayPauseButton();
    }

    public void RefreshAnimations()
    {
        if (_animator == null)
        {
            return;
        }

        var currentSelection = AnimationComboBox.SelectedItem as string;
        var animations = _animator.AnimationNames;
        AnimationComboBox.ItemsSource = null;
        AnimationComboBox.ItemsSource = animations;

        if (!string.IsNullOrEmpty(currentSelection))
        {
            for (var i = 0; i < animations.Count; i++)
            {
                if (animations[i] == currentSelection)
                {
                    AnimationComboBox.SelectedIndex = i;
                    return;
                }
            }
        }

        if (animations.Count > 0)
        {
            AnimationComboBox.SelectedIndex = 0;
        }
    }

    private void UpdateTimelineFromAnimator()
    {
        if (_animator == null)
        {
            return;
        }

        TimelineSlider.Value = _animator.NormalizedTime;
        UpdateCurrentTimeText();
    }

    private void OnAnimationSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_animator == null)
        {
            return;
        }

        var selectedAnimation = AnimationComboBox.SelectedItem as string;
        if (string.IsNullOrEmpty(selectedAnimation))
        {
            return;
        }

        _animator.Play(selectedAnimation);
        UpdateDurationText();
        UpdatePlayPauseButton();
    }

    private void OnTimelineValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        UpdateCurrentTimeText();
    }

    private void OnPlayPauseClicked(object? sender, RoutedEventArgs e)
    {
        if (_animator == null)
        {
            return;
        }

        if (_animator.IsPlaying && !_animator.IsPaused)
        {
            _animator.Pause();
        }
        else if (_animator.IsPaused)
        {
            _animator.Resume();
        }
        else
        {
            if (AnimationComboBox.SelectedItem is string anim)
            {
                _animator.Play(anim);
            }
        }

        UpdatePlayPauseButton();
    }

    private void OnStopClicked(object? sender, RoutedEventArgs e)
    {
        if (_animator == null)
        {
            return;
        }

        _animator.Stop();

        if (AnimationComboBox.SelectedItem is string anim)
        {
            _animator.Play(anim);
            _animator.Pause();
        }

        TimelineSlider.Value = 0;
        UpdatePlayPauseButton();
        UpdateCurrentTimeText();
    }

    private void OnSpeedValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_animator != null)
        {
            _animator.Speed = (float)SpeedSlider.Value;
        }
        SpeedText.Text = $"{SpeedSlider.Value:F1}x";
    }

    private void UpdatePlayPauseButton()
    {
        var isPlaying = _animator?.IsPlaying == true && !(_animator?.IsPaused ?? true);
        PlayPauseButton.Content = isPlaying ? "Pause" : "Play";
    }

    private void UpdateDurationText()
    {
        var duration = _animator?.Duration ?? 0f;
        DurationText.Text = FormatTime(duration);
    }

    private void UpdateCurrentTimeText()
    {
        var duration = _animator?.Duration ?? 0f;
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
    }
}
