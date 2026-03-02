using System.Text;
using DenOfIz;
using NiziKit.UI;

namespace DZForestDemo.UI;

public class ChatBox
{
    private readonly List<ChatMessage> _messages = [];
    private readonly List<string> _markupCache = [];
    private readonly List<string> _plainTextCache = [];

    public void Render()
    {
        using (NiziUi.Panel("ChatBox").Class("scrollPanel").Open())
        {
            using (NiziUi.Column("ChatMessages").GrowWidth().GrowHeight().Open())
            {
                for (var i = 0; i < _messages.Count; i++)
                {
                    var rowBg = i % 2 == 0 ? GameTheme.RowEven : GameTheme.RowOdd;

                    var elem = NiziUi.Panel("ChatMsg#" + i)
                        .Class("messageRow")
                        .Background(rowBg)
                        .Border(UiBorder.Vertical(1, GameTheme.Border));
                    if (NiziUi.WasRightClicked(elem.Id))
                    {
                        Clipboard.SetText(StringView.Intern(_plainTextCache[i]));
                    }

                    using (elem.Open())
                    {
                        NiziUi.StyledText(_markupCache[i], new UiTextStyle
                        {
                            Color = GameTheme.TextPrimary,
                            FontSize = GameTheme.FontBody
                        }, UiTextWrap.Words);
                    }
                }
            }
        }
    }

    public void AddMessage(ChatMessage message)
    {
        _messages.Add(message);
        _markupCache.Add(BuildMarkup(message));
        _plainTextCache.Add(BuildPlainText(message));
    }

    private static string BuildMarkup(ChatMessage msg)
    {
        var sb = new StringBuilder(128);
        sb.Append($"<color=#807060><size={GameTheme.FontTimestamp}>[{msg.Timestamp:HH:mm}] </size></color>");
        var (prefix, prefixColor) = msg.Type switch
        {
            ChatMessageType.Whisper => ("[Whisper] ", "#B888D8"),
            ChatMessageType.Party => ("[Party] ", "#68C888"),
            ChatMessageType.System => ("[System] ", "#6898C8"),
            _ => ((string?)null, (string?)null)
        };

        if (prefix != null)
        {
            sb.Append($"<color={prefixColor}><size={GameTheme.FontPrefix}>{prefix}</size></color>");
        }

        if (msg.Type != ChatMessageType.System)
        {
            sb.Append($"<color=#C8A84E>{msg.Character}: </color>");
        }
        var textColor = msg.Type == ChatMessageType.System ? "#6898C8" : "#E8DCC8";
        sb.Append($"<color={textColor}>{msg.Text}</color>");

        return sb.ToString();
    }

    private static string BuildPlainText(ChatMessage msg)
    {
        var sb = new StringBuilder(128);
        sb.Append($"[{msg.Timestamp:HH:mm}] ");
        if (msg.Type == ChatMessageType.Whisper)
        {
            sb.Append("[Whisper] ");
        }
        else if (msg.Type == ChatMessageType.Party)
        {
            sb.Append("[Party] ");
        }
        else if (msg.Type == ChatMessageType.System)
        {
            sb.Append("[System] ");
        }

        if (msg.Type != ChatMessageType.System)
        {
            sb.Append($"{msg.Character}: ");
        }

        sb.Append(msg.Text);
        return sb.ToString();
    }
}
