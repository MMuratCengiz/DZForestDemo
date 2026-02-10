using NiziKit.Animation;
using NiziKit.Editor.Theme;
using NiziKit.Editor.ViewModels;
using NiziKit.UI;

namespace NiziKit.Editor.UI.Panels;

public static class AnimatorEditorBuilder
{
    public static void BuildPlaybackControls(UiFrame ui, UiContext ctx, Animator animator,
        EditorViewModel editorVm, string sectionId)
    {
        var t = EditorTheme.Current;
        var controlsId = sectionId + "_AnimCtrl";
        using (ui.Panel(controlsId + "_Header")
            .Horizontal()
            .GrowWidth()
            .FitHeight()
            .Padding(0, 4)
            .AlignChildrenY(UiAlignY.Center)
            .Gap(4)
            .Open())
        {
            ui.Icon(FontAwesome.Play, t.Accent, t.IconSizeXS);
            ui.Text("Playback", new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
        }

        var animNames = GetAnimationNames(animator);
        if (animNames.Length > 0)
        {
            using (ui.Panel(controlsId + "_SelRow")
                .Horizontal()
                .GrowWidth()
                .FitHeight()
                .Gap(4)
                .AlignChildrenY(UiAlignY.Center)
                .Open())
            {
                ui.Text("Clip", new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });

                var currentAnim = animator.CurrentAnimation ?? animator.DefaultAnimation ?? "";
                var selectedIndex = Array.IndexOf(animNames, currentAnim);
                if (selectedIndex < 0)
                {
                    selectedIndex = 0;
                }

                if (Ui.Dropdown(ctx, controlsId + "_AnimDD", animNames)
                    .Background(t.SurfaceInset, t.Hover)
                    .TextColor(t.TextPrimary)
                    .FontSize(t.FontSizeCaption)
                    .CornerRadius(t.RadiusSmall)
                    .Padding(4, 3)
                    .GrowWidth()
                    .ItemHoverColor(t.Hover)
                    .DropdownBackground(t.PanelBackground)
                    .Placeholder("Select animation...")
                    .Show(ref selectedIndex))
                {
                    var animName = animNames[selectedIndex];
                    animator.Play(animName);
                }
            }

            using (ui.Panel(controlsId + "_Transport")
                .Horizontal()
                .GrowWidth()
                .FitHeight()
                .Gap(2)
                .Padding(0, 2)
                .AlignChildrenX(UiAlignX.Center)
                .AlignChildrenY(UiAlignY.Center)
                .Open())
            {
                var stopColor = animator.IsPlaying ? t.TextPrimary : t.TextDisabled;
                if (RenderTransportButton(ui, ctx, controlsId + "_Stop", FontAwesome.Stop, stopColor, t)
                    && animator.IsPlaying)
                {
                    animator.Stop();
                }

                if (animator is { IsPlaying: true, IsPaused: false })
                {
                    if (RenderTransportButton(ui, ctx, controlsId + "_Pause", FontAwesome.Pause, t.Accent, t))
                    {
                        animator.Pause();
                    }
                }
                else
                {
                    var playColor = animNames.Length > 0 ? t.Accent : t.TextDisabled;
                    if (RenderTransportButton(ui, ctx, controlsId + "_Play", FontAwesome.Play, playColor, t))
                    {
                        if (animator.IsPaused)
                        {
                            animator.Resume();
                        }
                        else if (animNames.Length > 0)
                        {
                            var animToPlay = animator.CurrentAnimation
                                ?? animator.DefaultAnimation
                                ?? animNames[0];
                            animator.Play(animToPlay);
                        }
                    }
                }
            }

            if (animator.IsPlaying || animator.IsPaused)
            {
                BuildProgressBar(ui, ctx, animator, controlsId, t);
            }

            using (ui.Panel(controlsId + "_OptionsRow")
                .Horizontal()
                .GrowWidth()
                .FitHeight()
                .Gap(8)
                .Padding(0, 2)
                .AlignChildrenY(UiAlignY.Center)
                .Open())
            {
                ui.Text("Loop", new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
                var loopNames = Enum.GetNames<LoopMode>();
                var loopIndex = (int)animator.CurrentLoopMode;

                if (Ui.Dropdown(ctx, controlsId + "_LoopDD", loopNames)
                    .Background(t.SurfaceInset, t.Hover)
                    .TextColor(t.TextPrimary)
                    .FontSize(t.FontSizeCaption)
                    .CornerRadius(t.RadiusSmall)
                    .Padding(4, 3)
                    .Width(UiSizing.Fixed(80))
                    .ItemHoverColor(t.Hover)
                    .DropdownBackground(t.PanelBackground)
                    .Show(ref loopIndex))
                {
                    if (animator.IsPlaying && animator.CurrentAnimation != null)
                    {
                        animator.Play(animator.CurrentAnimation, (LoopMode)loopIndex);
                    }
                }

                ui.Text("Speed", new UiTextStyle { Color = t.TextSecondary, FontSize = t.FontSizeCaption });
                var speed = animator.Speed;
                if (Ui.DraggableValue(ctx, controlsId + "_Speed")
                    .LabelWidth(0)
                    .Sensitivity(0.01f)
                    .Format("F2")
                    .FontSize(t.FontSizeCaption)
                    .Width(UiSizing.Fixed(50))
                    .ValueColor(t.InputBackground)
                    .ValueTextColor(t.InputText)
                    .Show(ref speed))
                {
                    animator.Speed = speed;
                }
            }
        }
        else
        {
            ui.Text("No animations available. Assign a skeleton first.",
                new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
        }
    }

    private static void BuildProgressBar(UiFrame ui, UiContext ctx, Animator animator,
        string controlsId, IEditorTheme t)
    {
        using (ui.Panel(controlsId + "_Progress")
            .Horizontal()
            .GrowWidth()
            .FitHeight()
            .Gap(6)
            .Padding(0, 2)
            .AlignChildrenY(UiAlignY.Center)
            .Open())
        {
            var timeText = $"{animator.Time:F1}s";
            ui.Text(timeText, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });

            var normalizedTime = animator.NormalizedTime;
            Ui.Slider(ctx, controlsId + "_Scrub")
                .Range(0f, 1f)
                .TrackColor(t.SurfaceInset)
                .FillColor(t.Accent)
                .ThumbColor(t.TextPrimary, t.Accent)
                .ShowValue(false)
                .FontSize(t.FontSizeCaption)
                .GrowWidth()
                .Show(ref normalizedTime);

            var durText = $"{animator.Duration:F1}s";
            ui.Text(durText, new UiTextStyle { Color = t.TextMuted, FontSize = t.FontSizeCaption });
        }
    }

    private static bool RenderTransportButton(UiFrame ui, UiContext ctx, string id,
        string icon, UiColor iconColor, IEditorTheme t)
    {
        var btn = Ui.Button(ctx, id, "")
            .Color(UiColor.Transparent, t.Hover, t.Active)
            .CornerRadius(t.RadiusMedium)
            .Padding(6, 4)
            .Border(0, UiColor.Transparent);

        using var scope = btn.Open();
        scope.Icon(icon, iconColor, t.IconSizeSmall);
        return btn.WasClicked();
    }

    private static string[] GetAnimationNames(Animator animator)
    {
        var names = animator.AnimationNames;
        if (names.Count == 0)
        {
            return [];
        }

        var result = new string[names.Count];
        for (var i = 0; i < names.Count; i++)
        {
            result[i] = names[i];
        }

        return result;
    }
}
