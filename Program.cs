using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OnlineLibrary.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// --- СЕКЦИЯ СЕРВИСОВ (До builder.Build) ---

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true; // Сделает JSON красивым в Postman
    });

builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Настройка JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- СЕКЦИЯ КОНСОЛЬНЫХ КОМАНД ---
if (args.Length > 0 && args[0] == "setrole")
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    
    if (args.Length < 3)
    {
        Console.WriteLine("Ошибка! Используйте формат: dotnet run setrole <email> <role>");
        return;
    }

    var email = args[1];
    var newRole = args[2]; 

    var user = context.Users.FirstOrDefault(u => u.Email == email);
    if (user != null)
    {
        user.Role = newRole;
        context.SaveChanges();
        Console.WriteLine($"[Успех]: Роль пользователя {email} изменена на {newRole}");
    }
    else
    {
        Console.WriteLine($"[Ошибка]: Пользователь с почтой {email} не найден в базе.");
    }
    return; 
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run(); 