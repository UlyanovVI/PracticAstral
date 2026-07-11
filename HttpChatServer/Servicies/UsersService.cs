using System.Security.Cryptography;
using System.Text;
using HttpChatShared.Models;
using HttpChatServer.Data;
using Microsoft.EntityFrameworkCore;

namespace HttpChatServer.Services;

public class UsersService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public UsersService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    //  Регистрация с паролем
    public async Task<(bool Success, string Message)> RegisterUserAsync(string userName, string password)
    {
        if (string.IsNullOrEmpty(userName))
            return (false, "Имя не может быть пустым");

        if (string.IsNullOrEmpty(password))
            return (false, "Пароль не может быть пустым");

        if (password.Length < 4)
            return (false, "Пароль должен быть не менее 4 символов");

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Проверяем, существует ли пользователь
        var exists = await dbContext.Users.AnyAsync(u => u.Name == userName);
        if (exists)
            return (false, $"Пользователь {userName} уже существует");

        // Хэшируем пароль
        var passwordHash = HashPassword(password);

        var user = new User {
            Name = userName,
            Password = passwordHash
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        Console.WriteLine($"Зарегистрирован новый пользователь: {userName}");
        return (true, "Регистрация успешна");
    }

    //  Вход (аутентификация)
    public async Task<(bool Success, string Message)> LoginUserAsync(string userName, string password)
    {
        if (string.IsNullOrEmpty(userName))
            return (false, "Имя не может быть пустым");

        if (string.IsNullOrEmpty(password))
            return (false, "Пароль не может быть пустым");

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Name == userName);
        if (user == null)
            return (false, "Пользователь не найден");

        // Проверяем пароль
        if (!VerifyPassword(password, user.Password))
            return (false, "Неверный пароль");

        Console.WriteLine($"Пользователь {userName} вошел в систему");
        return (true, "Вход выполнен успешно");
    }

    //  Проверка существования пользователя (ПРЯМО В БД)
    public async Task<bool> UserExistsAsync(string userName)
    {
        if (string.IsNullOrEmpty(userName))
            return false;

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Users.AnyAsync(u => u.Name == userName);
    }

    // Получение списка пользователей (ПРЯМО ИЗ БД)
    public async Task<List<string>> GetUsersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Users
            .Select(u => u.Name)
            .Distinct()
            .ToListAsync();
    }

    //  Получение пользователя по имени
    public async Task<User?> GetUserByNameAsync(string userName)
    {
        if (string.IsNullOrEmpty(userName))
            return null;

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Users.FirstOrDefaultAsync(u => u.Name == userName);
    }

    // Деактивация пользователя
    public async Task<bool> DeactivateUserAsync(string userName)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Name == userName);
        if (user == null)
            return false;

        dbContext.Users.Remove(user);  // Полное удаление
        await dbContext.SaveChangesAsync();

        Console.WriteLine($"Пользователь {userName} удален");
        return true;
    }

    // Получение всех пользователей (с паролями, для админа)
    public async Task<List<User>> GetAllUsersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Users.ToListAsync();
    }

    // ============ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ============

    // Методы хэширования паролей
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        var computedHash = HashPassword(password);
        return computedHash == hash;
    }
}
