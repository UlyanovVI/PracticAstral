using System.Net;
using System.Net.Sockets;
using System.Text;

async Task send_data(NetworkStream stream, string msg)
{
    byte[] data = Encoding.UTF8.GetBytes(msg);
    await stream.WriteAsync(data);
}
async Task<string> request_data(NetworkStream stream)
{
    byte[] buffer = new byte[1024];
    int count = await stream.ReadAsync(buffer);

    if (count == 0)
    {
        return "";
    }

    return Encoding.UTF8.GetString(buffer, 0, count);
}

int port = 0;
int server_port = 0;

/// ввод Ip, порта, порта клиента сервера и имени
Console.WriteLine("Введите свое имя: ");
string name = Console.ReadLine();

Console.WriteLine("Введите IP: ");
string ip = Console.ReadLine();

try
{
    Console.WriteLine("Введите порт, который надо слушать: ");
    string input = Console.ReadLine();
    port = int.Parse(input);
}
catch (Exception ex)
{
    Console.WriteLine($"Введите корректный порт! ({ex.Message})");
    return;
}

try
{
    Console.WriteLine("Введите порт сервера, к которому надо подключиться: ");
    string input = Console.ReadLine();
    server_port = int.Parse(input);
}
catch (Exception ex)
{
    Console.WriteLine($"Введите корректный порт сервера! ({ex.Message})");
    return;
}

TcpListener listener = new TcpListener(IPAddress.Any, port);
listener.Start();
Console.WriteLine($"Слушаю порт {port}, подключаюсь к {ip}:{server_port}");


TcpClient server = new TcpClient();
await server.ConnectAsync(ip, server_port);
Console.WriteLine($"Подключился к серверу.");


// Отправка на сервер имени и порта
string registration = $"{name}|{port}";
var stream = server.GetStream();
await send_data(stream, registration);

// === Чат ===

Task readTask = Task.Run(async () =>
{
    while (server.Connected)
    {
        try
        {
            string message = await request_data(stream);

            if (string.IsNullOrEmpty(message))
            {
                Console.WriteLine("Соединение с сервером разорвано");
                break;
            }
            if (message.StartsWith("new | "))
            {
                // Сообщение о новом пользователе
                Console.WriteLine($"\n[СИСТЕМА]: {message}");
            }
            else
            {
                // Обычное сообщение от пользователя
                Console.WriteLine($"\n{message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении сообщения: {ex.Message}");
            break;
        }
    }
});

while (server.Connected)
{
    string message = Console.ReadLine() ?? "";

    if (message.ToLower() == "exit")
    {
        Console.WriteLine("Выход...");
        break;
    }

    if (string.IsNullOrEmpty(message))
    {
        continue;
    }

    try
    {
        await send_data(stream, message);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка отправки сообщения: {ex.Message}");
        break;
    }
}

server.Close();
listener.Stop();
Console.WriteLine("Соединение закрыто");
