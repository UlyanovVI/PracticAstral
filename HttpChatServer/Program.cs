using HttpChatServer.Data;
using HttpChatServer.Services;
using HttpChatShared.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Регистрация DbContext (Scoped по умолчанию)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=chat.db"));

// 2. UsersService — Singleton (кэш пользователей сохраняется)
builder.Services.AddSingleton<UsersService>();

// 3. ChatService — Scoped (работает с БД через DbContext)
builder.Services.AddScoped<ChatService>();

// 4. Контроллеры
builder.Services.AddControllers();

// 5. Swagger
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 6. Создание базы данных
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
    Console.WriteLine($"База данных: {Path.GetFullPath("chat.db")}");
}

// 7. Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 8. Глобальное логирование
app.Use(async (context, next) => {
    Console.WriteLine($"{context.Request.Method} {context.Request.Path}");
    await next();
});

// 9. Маршрутизация
app.UseRouting();
app.MapControllers();

app.Run();
