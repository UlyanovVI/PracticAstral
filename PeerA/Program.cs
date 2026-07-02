using System.Net;
using System.Net.Sockets;
using System.Text;

const int port = 30000;
const int peer_port = 30001;
const string peer_ip = "127.0.0.1";

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

await send_stream.WriteAsync(Encoding.UTF8.GetBytes("Как тебя зовут?"));

var buffer = new byte[1024];
var count = await recv_stream.ReadAsync(buffer);
var peer_name = Encoding.UTF8.GetString(buffer, 0, count);
Console.WriteLine($"Мой собеседник: {peer_name}");

Console.Write("Введите ваше имя: ");
var my_name = Console.ReadLine();
await send_stream.WriteAsync(Encoding.UTF8.GetBytes(my_name));

Console.WriteLine($"Пишите сообщения (exit — выход).\n");

// === Чат ===

_ = Task.Run(async () =>
{
    var chatBuffer = new byte[1024];
    while (true)
    {
        var chatCount = await recv_stream.ReadAsync(chatBuffer);
        if (chatCount == 0) break;
        var msg = Encoding.UTF8.GetString(chatBuffer, 0, chatCount);
        Console.WriteLine($"[{peer_name}]: {msg}");
    }
});

while (true)
{
    var line = Console.ReadLine();
    if (string.IsNullOrEmpty(line)) continue;
    if (line == "exit") break;

    var bytes = Encoding.UTF8.GetBytes(line);
    await send_stream.WriteAsync(bytes);
}

Console.WriteLine($"[{my_name}] Выход");