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
    private bool _isPlaying;
    private float _playbackSpeed = 1.0f;
    private string? _currentAnimation;

    public bool IsPlaying => _isPlaying;
    public float PlaybackSpeed => _playbackSpeed;

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
            _isPlaying = false;
            return;
        }

        if (!animator.IsInitialized)
        {
            animator.Initialize();
        }

        var animations = GetAvailableAnimations();
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

    private IReadOnlyList<string> GetAvailableAnimations()
    {
        if (_animator?.Skeleton == null)
        {
            return Array.Empty<string>();
        }

        return _animator.Skeleton.AnimationNames;
    }

    public void Update(float deltaTime)
    {
        if (_animator == null || !_isPlaying)
        {
            return;
        }

        _animator.Update(deltaTime * _playbackSpeed);
        UpdateTimelineFromAnimator();
    }

    private void UpdateTimelineFromAnimator()
    {
        if (_animator == null)
        {
            return;
        }

        TimelineSlider.Value = _animator.GetCurrentStateNormalizedTime();
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

        _currentAnimation = selectedAnimation;
        _animator.Play(selectedAnimation);
        _isPlaying = true;

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

        if (_isPlaying)
        {
            _isPlaying = false;
        }
        else
        {
            if (_currentAnimation == null && AnimationComboBox.SelectedItem is string anim)
            {
                _currentAnimation = anim;
                _animator.Play(anim);
            }
            _isPlaying = true;
        }

        UpdatePlayPauseButton();
    }

    private void OnStopClicked(object? sender, RoutedEventArgs e)
    {
        if (_animator == null)
        {
            return;
        }

        _isPlaying = false;
        if (_currentAnimation != null)
        {
            _animator.Play(_currentAnimation);
        }
        TimelineSlider.Value = 0;
        UpdatePlayPauseButton();
        UpdateCurrentTimeText();
    }

    private void OnSpeedValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        _playbackSpeed = (float)SpeedSlider.Value;
        SpeedText.Text = $"{SpeedSlider.Value:F1}x";
    }

    private void UpdatePlayPauseButton()
    {
        PlayPauseButton.Content = _isPlaying ? "Pause" : "Play";
    }

    private void UpdateDurationText()
    {
        var state = _animator?.GetCurrentState();
        var duration = state?.Clip?.Duration ?? 1f;
        DurationText.Text = FormatTime(duration);
    }

    private void UpdateCurrentTimeText()
    {
        var state = _animator?.GetCurrentState();
        var duration = state?.Clip?.Duration ?? 1f;
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
        _isPlaying = false;
    }
}
