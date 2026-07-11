using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LogiTrack.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("=== APPLICATION STARTUP ===");
var jwtTestSettings = builder.Configuration.GetSection("Jwt");
Console.WriteLine($"JWT Section exists: {jwtTestSettings.Exists()}");
Console.WriteLine($"JWT:Key value: {jwtTestSettings["Key"] ?? "NOT FOUND"}");
Console.WriteLine($"JWT:Issuer value: {jwtTestSettings["Issuer"] ?? "NOT FOUND"}");
Console.WriteLine($"JWT:Audience value: {jwtTestSettings["Audience"] ?? "NOT FOUND"}");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<LogiTrackContext>(options =>
    options.UseSqlite("Data Source=logitrack.db"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("Jwt");
        var key = jwtSettings["Key"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        Console.WriteLine($"JWT Config Read from appsettings:");
        Console.WriteLine($"  Key is null: {key == null}");
        Console.WriteLine($"  Issuer: {issuer ?? "NULL"}");
        Console.WriteLine($"  Audience: {audience ?? "NULL"}");

        if (string.IsNullOrEmpty(key))
            key = "ThisIsAReallyLongAndSecureJwtSigningKeyForLogiTrack2026!";
        if (string.IsNullOrEmpty(issuer))
            issuer = "LogiTrack";
        if (string.IsNullOrEmpty(audience))
            audience = "LogiTrackUsers";

        Console.WriteLine($"JWT Config - Final Values:");
        Console.WriteLine($"  Issuer: {issuer}");
        Console.WriteLine($"  Audience: {audience}");
        Console.WriteLine($"  Key Length: {key.Length} bytes");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"JWT Validation Failed: {context.Exception.Message}");
                if (context.Exception.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {context.Exception.InnerException.Message}");
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("JWT Token Validated Successfully");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<LogiTrackContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<AuthenticationOptions>(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    var authHeader = context.Request.Headers["Authorization"].ToString();
    Console.WriteLine($"Authorization Header Received: {(string.IsNullOrEmpty(authHeader) ? "NONE" : authHeader.Substring(0, Math.Min(50, authHeader.Length)) + "...")}");
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var sampleItem = new InventoryItem
{
    ItemId = 1,
    Name = "Pallet Jack",
    Quantity = 12,
    Location = "Warehouse A"
};

sampleItem.DisplayInfo();

var order = new Order
{
    OrderId = 1001,
    CustomerName = "Samir",
    DatePlaced = new DateTime(2025, 4, 5)
};

order.AddItem(sampleItem);
order.AddItem(new InventoryItem
{
    ItemId = 2,
    Name = "Forklift Battery",
    Quantity = 1,
    Location = "Warehouse B"
});

order.RemoveItem(1);

System.Console.WriteLine(order.GetOrderSummary());

using (var context = new LogiTrackContext(new DbContextOptionsBuilder<LogiTrackContext>()
    .UseSqlite("Data Source=logitrack.db")
    .Options))
{
    // Add test inventory item if none exist
    if (!context.InventoryItems.Any())
    {
        context.InventoryItems.Add(new InventoryItem
        {
            Name = "Pallet Jack",
            Quantity = 12,
            Location = "Warehouse A"
        });

        context.SaveChanges();
    }

    // Retrieve and print inventory to confirm
    var items = context.InventoryItems.ToList();
    foreach (var item in items)
    {
        item.DisplayInfo(); // Should print: Item: Pallet Jack | Quantity: 12 | Location: Warehouse A
    }
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapGet("/api/auth/debug-token", (HttpContext context) =>
{
    var user = context.User;
    if (user?.Identity?.IsAuthenticated != true)
    {
        return Results.Json(new { authenticated = false, message = "Not authenticated" }, statusCode: 401);
    }

    var claims = user.Claims.Select(c => new { type = c.Type, value = c.Value }).ToList();
    return Results.Json(new { authenticated = true, claims });
}).WithName("DebugToken").Produces(200).Produces(401);

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
