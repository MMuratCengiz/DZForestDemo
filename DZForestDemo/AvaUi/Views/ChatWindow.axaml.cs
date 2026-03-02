using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using DZForestDemo.AvaUi.Components;
using DZForestDemo.AvaUi.Models;
using DZForestDemo.AvaUi.Services;

namespace DZForestDemo.AvaUi.Views;

public partial class ChatWindow : Panel
{
    private const double FontBody = 14;
    private const double FontTimestamp = 12;
    private const double FontPrefix = 13;

    private ChatService? _chatService;
    private CombatLogService? _combatLogService;
    private int _chatRowIndex;
    private int _combatRowIndex;

    public ChatWindow()
    {
        InitializeComponent();
    }

    public void BindChatService(ChatService service)
    {
        _chatService = service;

        foreach (var msg in service.Messages)
        {
            AddChatMessageUi(msg);
        }

        service.Messages.CollectionChanged += OnChatMessagesChanged;
        ChatInput.KeyDown += OnChatInputKeyDown;
    }

    public void BindCombatLogService(CombatLogService service)
    {
        _combatLogService = service;

        foreach (var entry in service.Entries)
        {
            AddCombatEntryUi(entry);
        }

        service.Entries.CollectionChanged += OnCombatEntriesChanged;
    }

    public void ShowTooltip(HoverText source)
    {
        var title = source.TooltipTitle;
        var content = source.TooltipContent;
        var icon = source.TooltipIcon;

        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(content))
        {
            return;
        }

        var pos = source.TranslatePoint(new Point(0, 0), this);
        if (!pos.HasValue)
        {
            return;
        }

        var x = Math.Max(4, Math.Min(pos.Value.X, Width - 290));
        var y = pos.Value.Y - 8;

        TooltipIconText.Text = icon ?? "";
        TooltipIconText.IsVisible = !string.IsNullOrEmpty(icon);
        TooltipTitleText.Text = title ?? "";
        TooltipHeader.IsVisible = !string.IsNullOrEmpty(title);

        var hasDescription = !string.IsNullOrEmpty(content);
        TooltipSeparator.IsVisible = !string.IsNullOrEmpty(title) && hasDescription;
        TooltipDescriptionText.Text = content ?? "";
        TooltipDescriptionText.IsVisible = hasDescription;

        TooltipOverlay.IsVisible = true;

        TooltipOverlay.Measure(new Size(280, double.PositiveInfinity));
        var tooltipHeight = TooltipOverlay.DesiredSize.Height;
        y = Math.Max(4, y - tooltipHeight);

        Canvas.SetLeft(TooltipOverlay, x);
        Canvas.SetTop(TooltipOverlay, y);
    }

    public void HideTooltip()
    {
        TooltipOverlay.IsVisible = false;
    }

    private void OnChatInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(ChatInput.Text))
        {
            _chatService?.AddMessage("You", ChatInput.Text);
            ChatInput.Text = "";
            e.Handled = true;
        }
    }

    private void OnChatMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems != null:
                foreach (ChatMessage msg in e.NewItems)
                {
                    AddChatMessageUi(msg);
                }

                break;
            case NotifyCollectionChangedAction.Remove when e.OldStartingIndex == 0:
                if (ChatMessages.Children.Count > 0)
                {
                    ChatMessages.Children.RemoveAt(0);
                }

                break;
            case NotifyCollectionChangedAction.Reset:
                ChatMessages.Children.Clear();
                _chatRowIndex = 0;
                break;
        }
    }

    private void OnCombatEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems != null:
                foreach (CombatLogEntry entry in e.NewItems)
                {
                    AddCombatEntryUi(entry);
                }

                break;
            case NotifyCollectionChangedAction.Remove when e.OldStartingIndex == 0:
                if (CombatEntries.Children.Count > 0)
                {
                    CombatEntries.Children.RemoveAt(0);
                }

                break;
            case NotifyCollectionChangedAction.Reset:
                CombatEntries.Children.Clear();
                _combatRowIndex = 0;
                break;
        }
    }

    private void AddChatMessageUi(ChatMessage msg)
    {
        var typeBrush = GetChatTypeBrush(msg.Type);
        var textBrush = GetBrush("GameTextPrimary") ?? Brushes.White;
        var goldBrush = GetBrush("GameTextGold") ?? Brushes.Gold;
        var mutedBrush = GetBrush("GameTextMuted") ?? Brushes.Gray;
        var rowBg = GetRowBackground(_chatRowIndex++);

        var row = new DockPanel
        {
            Margin = new Thickness(0),
            Background = rowBg
        };

        var textBlock = new SelectableTextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontFamily = FindFontFamily(),
            FontSize = FontBody,
            Padding = new Thickness(0, 4, 4, 4)
        };

        textBlock.Inlines!.Add(new Run
        {
            Text = $"[{msg.Timestamp:HH:mm}] ",
            Foreground = mutedBrush,
            FontSize = FontTimestamp
        });

        var prefix = msg.Type switch
        {
            ChatMessageType.Whisper => "[Whisper] ",
            ChatMessageType.Party => "[Party] ",
            ChatMessageType.System => "[System] ",
            _ => null
        };

        if (prefix != null)
        {
            textBlock.Inlines.Add(new Run
            {
                Text = prefix,
                Foreground = typeBrush,
                FontSize = FontPrefix,
                FontStyle = FontStyle.Italic
            });
        }

        if (msg.Type != ChatMessageType.System)
        {
            textBlock.Inlines.Add(new Run
            {
                Text = $"{msg.Character}: ",
                Foreground = goldBrush,
                FontWeight = FontWeight.Bold
            });
        }

        textBlock.Inlines.Add(new Run
        {
            Text = msg.Text,
            Foreground = msg.Type == ChatMessageType.System ? typeBrush : textBrush
        });

        row.Children.Add(textBlock);
        ChatMessages.Children.Add(row);
        ChatScrollViewer.ScrollToEnd();
    }

    private void AddCombatEntryUi(CombatLogEntry entry)
    {
        var mutedBrush = GetBrush("GameTextMuted") ?? Brushes.Gray;
        var textBrush = GetBrush("GameTextPrimary") ?? Brushes.White;
        var rowBg = GetRowBackground(_combatRowIndex++);

        var wrapBorder = new Border
        {
            Background = rowBg,
            Padding = new Thickness(6, 4)
        };
        var wrap = new WrapPanel
        {
            Orientation = Orientation.Horizontal
        };
        wrapBorder.Child = wrap;

        wrap.Children.Add(new TextBlock
        {
            Text = $"[{entry.Timestamp:HH:mm:ss}] ",
            Foreground = mutedBrush,
            FontSize = FontTimestamp,
            FontFamily = FindFontFamily(),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 2, 0)
        });

        foreach (var segment in entry.Segments)
        {
            if (segment.IsHoverable)
            {
                var hover = new HoverText
                {
                    Text = segment.Text,
                    TooltipTitle = segment.TooltipTitle,
                    TooltipContent = segment.TooltipContent,
                    TooltipIcon = segment.TooltipIcon,
                    FontSize = FontBody,
                    FontFamily = FindFontFamily(),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = GetSegmentBrush(segment) ?? textBrush,
                    Margin = new Thickness(0, 0, 1, 0)
                };

                hover.PointerEntered += (_, _) => ShowTooltip(hover);
                hover.PointerExited += (_, _) => HideTooltip();

                wrap.Children.Add(hover);
            }
            else
            {
                wrap.Children.Add(new TextBlock
                {
                    Text = segment.Text,
                    FontSize = FontBody,
                    FontFamily = FindFontFamily(),
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = GetSegmentBrush(segment) ?? textBrush
                });
            }
        }

        CombatEntries.Children.Add(wrapBorder);
        CombatScrollViewer.ScrollToEnd();
    }

    private IBrush GetRowBackground(int index)
    {
        var key = index % 2 == 0 ? "RowEven" : "RowOdd";
        return GetBrush(key) ?? Brushes.Transparent;
    }

    private IBrush? GetSegmentBrush(TextSegment segment)
    {
        return segment.ColorResourceKey != null ? GetBrush(segment.ColorResourceKey) : null;
    }

    private IBrush GetChatTypeBrush(ChatMessageType type)
    {
        var key = type switch
        {
            ChatMessageType.System => "ChatSystem",
            ChatMessageType.Whisper => "ChatWhisper",
            ChatMessageType.Party => "ChatParty",
            _ => "ChatNormal"
        };
        return GetBrush(key) ?? Brushes.White;
    }

    private IBrush? GetBrush(string key)
    {
        if (this.TryFindResource(key, this.ActualThemeVariant, out var resource) && resource is IBrush brush)
        {
            return brush;
        }

        return null;
    }

    private FontFamily FindFontFamily()
    {
        if (this.TryFindResource("GameFont", this.ActualThemeVariant, out var resource) && resource is FontFamily ff)
        {
            return ff;
        }

        return FontFamily.Default;
    }
}
