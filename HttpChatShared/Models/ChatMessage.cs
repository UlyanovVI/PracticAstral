namespace HttpChatShared.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public string From { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
