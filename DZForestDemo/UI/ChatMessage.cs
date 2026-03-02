namespace DZForestDemo.UI;

public enum ChatMessageType
{
    Normal,
    System,
    Whisper,
    Party
}

public record ChatMessage(
    string Character,
    string Text,
    ChatMessageType Type = ChatMessageType.Normal)
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
