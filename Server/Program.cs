using System.Net;
using System.Net.Sockets;
using System.Text;

async Task send_data(string msg, Dictionary<string, (string ip, int port, TcpClient Client)> clients)
{
    byte[] message_data = Encoding.UTF8.GetBytes(msg);

    lock (clients)
    {
        foreach (var other in clients.Values)
        {
            try
            {
                other.Client.GetStream().Write(message_data);
            }
            catch { }
        }
    }
}

async Task<string> request_data(NetworkStream stream)
{
    byte[] buffer = new byte[1024];
    int count = await stream.ReadAsync(buffer);

    if (count == 0)
    {
        return "";
    }
    else
    {
        return Encoding.UTF8.GetString(buffer, 0, count);
    }
}

var clients = new Dictionary<string, (string ip, int port, TcpClient Client)>();
var client_names = new Dictionary<TcpClient, string>();

string IP = "127.0.0.1";
int server_port = 30000;

TcpListener listener = new TcpListener(IPAddress.Any, server_port);
listener.Start();
Console.WriteLine($"Сервер запущен на порту {server_port}");

while (true)
{
    TcpClient client = await listener.AcceptTcpClientAsync();


    Task handle_cli = Task.Run(async () =>
    {
        var stream = client.GetStream();

        try
        {
            string data = await request_data(stream);

            string[] parts = data.Split('|');
            string name = parts[0];
            int port = int.Parse(parts[1]);


            lock (clients)
            {
                clients[name] = (IP, port, client);
                client_names[client] = name;
                Console.WriteLine($"Клиент {name} подключен. Всего {clients.Count}");
            }

            string new_client_msg = $"new | {name} | {IP} | {port}";
            await send_data(new_client_msg, clients);

            while (client.Connected)
            {
                try
                {
                    //считывания сообщения от участников чата
                    string req_data = await request_data(stream);

                    if (req_data == "")
                    {
                        Console.WriteLine($"Клиент {name} отключился");
                        break;
                    }

                    //формирование сообщения и отправка всем участникам
                    string formatted_message = $"[{name}] {req_data}";
                    await send_data(formatted_message, clients);
                }
                catch
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
        finally
        {
            lock (clients)
            {
                if (client_names.TryGetValue(client, out string? name))
                {
                    clients.Remove(name);
                    Console.WriteLine($"Клиент {name} отключился. Всего: {clients.Count}");
                }
                client_names.Remove(client);
            }
            client.Close();
        }
    });
}