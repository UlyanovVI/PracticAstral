using Microsoft.AspNetCore.Mvc;
using HttpChatShared.Models;
using HttpChatServer.Services;

namespace HttpChatServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly UsersService _usersService;
    private readonly ChatService _chatService;

    public ChatController(UsersService usersService, ChatService chatService)
    {
        _usersService = usersService;
        _chatService = chatService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Name))
            return BadRequest("Имя не может быть пустым");

        if (string.IsNullOrEmpty(request.Password))
            return BadRequest("Пароль не может быть пустым");

        var (success, message) = await _usersService.RegisterUserAsync(request.Name, request.Password);

        if (!success)
            return BadRequest(message);

        _chatService.AddSystemMessage($"Пользователь {request.Name} присоединился к чату");

        return Ok(new { message = $"Пользователь {request.Name} зарегистрирован" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] RegisterRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Name))
            return BadRequest("Имя не может быть пустым");

        if (string.IsNullOrEmpty(request.Password))
            return BadRequest("Пароль не может быть пустым");

        var (success, message) = await _usersService.LoginUserAsync(request.Name, request.Password);

        if (!success)
            return Unauthorized(message);

        return Ok(new { message = $"Добро пожаловать, {request.Name}" });
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.From) || string.IsNullOrEmpty(request.Text))
            return BadRequest("Неверный формат сообщения");

        var exists = await _usersService.UserExistsAsync(request.From);
        if (!exists)
            return BadRequest("Пользователь не зарегистрирован или не активен");

        _chatService.SendMessageToAll(request.From, request.Text);
        return Ok(new { message = "Сообщение отправлено" });
    }

    [HttpGet("messages/{userName}")]
    public async Task<IActionResult> GetMessages(string userName, [FromQuery] int? lastCount)
    {
        var exists = await _usersService.UserExistsAsync(userName);
        if (!exists)
            return NotFound($"Пользователь {userName} не найден");

        var messages = _chatService.GetMessages(userName, lastCount);
        return Ok(messages);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _usersService.GetUsersAsync();
        return Ok(users);
    }

    [HttpDelete("users/{userName}")]
    public async Task<IActionResult> DeleteUser(string userName)
    {
        if (string.IsNullOrEmpty(userName))
            return BadRequest("Имя не может быть пустым");

        var result = await _usersService.DeactivateUserAsync(userName);
        if (!result)
            return NotFound($"Пользователь {userName} не найден");

        return Ok(new { message = $"Пользователь {userName} удален" });
    }
}
