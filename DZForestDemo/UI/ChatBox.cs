using NiziKit.UI;

namespace DZForestDemo.UI;

public class ChatBox
{
    private readonly List<string> _chatMessages = [];

    public void Render()
    {
        var i = 0;
        using (NiziUi.Panel("ChatBox")
                   .GrowHeight()
                   .GrowWidth()
                   .Background(UiColor.Rgba(255, 255, 255, 20))
                   .Border(1, UiColor.Rgb(100, 100, 100))
                   .CornerRadius(4)
                   .ScrollVertical()
                   .Open())
        {
            using (NiziUi.Column("ChatMessages").GrowHeight().Open())
            {
                foreach (var chatMessage in _chatMessages)
                {
                    using (NiziUi.Row("ChatMessage#" + i++)
                               .Background(255, 255, 255, 0)
                               .Border(UiBorder.Vertical(1, UiColor.Rgb(100, 100, 100)))
                               .Padding(4)
                               .Open())
                    {
                        NiziUi.StyledText(chatMessage);
                    }
                }
            }
        }
    }

    public void AddMessage(string message)
    {
        _chatMessages.Add(message);
    }
}
