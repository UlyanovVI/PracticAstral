using HttpChatClient.Services;
using HttpChatShared.Models;

Console.Write("Введите ваше имя: ");
string userName = Console.ReadLine() ?? "User";

Console.Write("Введите URL сервера: ");
string baseUrl = Console.ReadLine() ?? "http://localhost:5097";

Console.Write("Введите пароль: ");
string password = Console.ReadLine() ?? "";

// Создаем обертку для HttpClient
using var chatClient = new ChatClient(baseUrl);

// Регистрация
Console.WriteLine("Регистрация...");
var (success, message, error) = await chatClient.RegisterAsync(userName, password);

if (!success)
{
    Console.WriteLine($"Ошибка регистрации: {error}");
    return;
}

Console.WriteLine($"Зарегистрирован как {userName}!");

// Фоновый поток для получения сообщений
_ = Task.Run(async () => {
    int lastMessageCount = 0;

    while (true)
    {
        try
        {
            var messages = await chatClient.GetMessagesAsync(userName, 10);

            if (messages.Count > lastMessageCount)
            {
                for (int i = lastMessageCount; i < messages.Count; i++)
                {
                    var msg = messages[i];

                    if (msg.From == "Система")
                    {
                        Console.WriteLine($"\n[СИСТЕМА]: {msg.Text}");
                    }
                    else
                    {
                        Console.WriteLine($"\n[{msg.From}] {msg.Text}");
                    }
                }

                lastMessageCount = messages.Count;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка получения сообщений: {ex.Message}");
        }

        await Task.Delay(1000);
    }
});

// Основной цикл отправки сообщений
Console.WriteLine("\nЧат запущен! Введите сообщение или 'exit' для выхода:");

while (true)
{
    string msg = Console.ReadLine() ?? "";

    if (msg.ToLower() == "exit")
        break;

    if (string.IsNullOrEmpty(msg))
        continue;

    var (sendSuccess, sendError) = await chatClient.SendMessageAsync(userName, msg);

    if (!sendSuccess)
    {
        Console.WriteLine($"Ошибка отправки: {sendError}");
    }
}

Console.WriteLine("Выход...");
