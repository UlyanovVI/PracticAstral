using System.Net;
using System.Net.Sockets;
using System.Text;

const string peer_ip = "";
int port = 0;
int peer_port = 0;

async Task<string> send_data(NetworkStream stream)
{
    string message = Console.ReadLine();
    byte[] data = Encoding.UTF8.GetBytes(message);

    if (data.Length > 1024)
    {
        Console.WriteLine($"Сообщение слишком длинное! Обрезаем до 1024 байт.");

        // Обрезаем до 1024 байт
        byte[] truncated = new byte[1024];
        Array.Copy(data, truncated, 1024);
        await stream.WriteAsync(truncated);
    }
    else
    {
        await stream.WriteAsync(data);
    }
    return message;
}
async Task<string> request_data(NetworkStream stream)
{
    byte[] buffer = new byte[1024];
    int count = await stream.ReadAsync(buffer);
    if (count == 0) return string.Empty;
    return Encoding.UTF8.GetString(buffer, 0, count);
}

Console.WriteLine("Введите IP: ");
string peer_ip = Console.ReadLine();

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
    Console.WriteLine("Введите порт, к которому надо подключиться: ");
    string input = Console.ReadLine();
    peer_port = int.Parse(input);
}
catch (Exception ex)
{
    Console.WriteLine($"Введите корректный порт к которому нужно подключиться! ({ex.Message})");
    return;
}


TcpListener listener = new TcpListener(IPAddress.Any, port);
listener.Start();
Console.WriteLine($"Слушаю порт {port}, подключаюсь к {peer_ip}:{peer_port}");

TcpClient outgoing = new TcpClient();
for (int i = 0; i < 10; i++)
{
    try
    {
        await outgoing.ConnectAsync(peer_ip, peer_port);
        break;
    }
    catch
    {
        Console.WriteLine($"Пир пока не отвечает");
        await Task.Delay(1000);
    }
}

using var incoming = await listener.AcceptTcpClientAsync();
Console.WriteLine($"Подключился к пиру.");

using var send_stream = outgoing.GetStream();
using var recv_stream = incoming.GetStream();

// === Обмен именами ===

Console.Write("Введите ваше имя: ");

await send_data(send_stream);

var peer_name = await request_data(recv_stream);

Console.WriteLine($"Мой собеседник: {peer_name}");

Console.WriteLine($"Пишите сообщения (exit — выход).\n");

// === Чат ===

async Task ReadMessages()
{
    var chatBuffer = new byte[1024];
    while (true)
    {
        string msg = await request_data(recv_stream);

        if (string.IsNullOrEmpty(msg))
        {
            Console.WriteLine("Собеседник отключился.");
            break;
        }

        Console.WriteLine($"[{peer_name}]: {msg}");
    }
}

ReadMessages();

while (true)
{
    string msg = await send_data(send_stream);
    if (string.IsNullOrEmpty(msg)) continue;
    if (msg == "exit") break;
}

Console.WriteLine($"Выход");
