using HttpChatShared.Models;
using HttpChatServer.Data;

namespace HttpChatServer.Services;

public class ChatService
{
    private readonly AppDbContext _dbContext;

    public ChatService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void SendMessageToAll(string from, string text)
    {
        var msg = new ChatMessage {
            From = from,
            Text = text,
            Timestamp = DateTime.Now
        };

        _dbContext.Messages.Add(msg);
        _dbContext.SaveChanges();

        Console.WriteLine($"Сообщение от {from} сохранено в БД");
    }

    public List<ChatMessage> GetMessages(string userName, int? lastCount = null)
    {
        var query = _dbContext.Messages.AsQueryable();

        if (!string.IsNullOrEmpty(userName))
        {
            query = query.Where(m => m.From == userName || m.From == "Система");
        }
        else
        {
            return new List<ChatMessage>();
        }

        query = query.OrderByDescending(m => m.Timestamp);

        if (lastCount.HasValue && lastCount.Value > 0)
        {
            query = query.Take(lastCount.Value);
        }

        return query.OrderBy(m => m.Timestamp).ToList();
    }

    public List<ChatMessage> GetAllMessages()
    {
        return _dbContext.Messages
            .OrderBy(m => m.Timestamp)
            .ToList();
    }

    public void AddSystemMessage(string text)
    {
        var systemMsg = new ChatMessage {
            From = "Система",
            Text = text,
            Timestamp = DateTime.Now
        };

        _dbContext.Messages.Add(systemMsg);
        _dbContext.SaveChanges();

        Console.WriteLine($"Системное сообщение: {text}");
    }
}
