using System.Text;
using Newtonsoft.Json;
using HttpChatShared.Models;

namespace HttpChatClient.Services;

public class ChatClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ChatClient(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    }

    // Регистрация пользователя
    public async Task<(bool Success, string Message, string? Error)> RegisterAsync(string userName, string password)
    {
        try
        {
            var request = new { Name = userName, Password = password };

            var response = await PostAsync("/api/chat/register", request);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(json);
                return (true, result?.message ?? "Регистрация успешна", null);
            }

            var error = await response.Content.ReadAsStringAsync();
            return (false, "", error);
        }
        catch (Exception ex)
        {
            return (false, "", ex.Message);
        }
    }

    // Отправка сообщения
    public async Task<(bool Success, string? Error)> SendMessageAsync(string from, string text)
    {
        try
        {
            var request = new { From = from, Text = text };
            var response = await PostAsync("/api/chat/send", request);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // Получение сообщений
    public async Task<List<ChatMessage>> GetMessagesAsync(string userName, int? lastCount = null)
    {
        try
        {
            var url = $"/api/chat/messages/{userName}";
            if (lastCount.HasValue)
                url += $"?lastCount={lastCount.Value}";

            var response = await GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<ChatMessage>>(json) ?? new List<ChatMessage>();
            }

            return new List<ChatMessage>();
        }
        catch
        {
            return new List<ChatMessage>();
        }
    }

    // Приватные методы для работы с HTTP

    private async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
    {
        var json = JsonConvert.SerializeObject(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _httpClient.PostAsync(url, content);
    }

    private async Task<HttpResponseMessage> GetAsync(string url)
    {
        return await _httpClient.GetAsync(url);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
