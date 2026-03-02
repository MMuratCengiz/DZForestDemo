using System.Collections.ObjectModel;
using DZForestDemo.AvaUi.Models;

namespace DZForestDemo.AvaUi.Services;

public class ChatService
{
    private const int MaxMessages = 500;

    public ObservableCollection<ChatMessage> Messages { get; } = [];

    public void AddMessage(string character, string text, ChatMessageType type = ChatMessageType.Normal)
    {
        Messages.Add(new ChatMessage(character, text, type));
        TrimExcess();
    }

    public void AddSystemMessage(string text)
    {
        Messages.Add(new ChatMessage("System", text, ChatMessageType.System));
        TrimExcess();
    }

    private void TrimExcess()
    {
        while (Messages.Count > MaxMessages)
        {
            Messages.RemoveAt(0);
        }
    }
}
