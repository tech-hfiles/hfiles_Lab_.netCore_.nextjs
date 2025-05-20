using HFiles_Backend.API.Services;
using HFiles_Backend.Domain.Entities.Labs;
using HFiles_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using MySqlConnector;
using HFiles_Backend.API.Settings;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Load .env and inject into Configuration
DotNetEnv.Env.Load();

var connectionString = $"Server={Environment.GetEnvironmentVariable("DB_HOST")};" +
                       $"Port={Environment.GetEnvironmentVariable("DB_PORT")};" +
                       $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                       $"User={Environment.GetEnvironmentVariable("DB_USER")};" +
                       $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};";

Console.WriteLine("DB_HOST: " + Environment.GetEnvironmentVariable("DB_HOST"));
Console.WriteLine("DB_PORT: " + Environment.GetEnvironmentVariable("DB_PORT"));
Console.WriteLine("DB_NAME: " + Environment.GetEnvironmentVariable("DB_NAME"));
Console.WriteLine("DB_USER: " + Environment.GetEnvironmentVariable("DB_USER"));
Console.WriteLine("DB_PASSWORD: " + Environment.GetEnvironmentVariable("DB_PASSWORD"));

try
{
    using (var connection = new MySqlConnection(connectionString))
    {
        connection.Open();
        Console.WriteLine("✅ Database connection successful!");
    }
}
catch (Exception ex)
{
    Console.WriteLine("❌ Connection failed: " + ex.Message);
}

builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;


// Load .env and inject into SMTP Configuration
builder.Configuration["Smtp:Host"] = Environment.GetEnvironmentVariable("SMTP_HOST");
builder.Configuration["Smtp:Port"] = Environment.GetEnvironmentVariable("SMTP_PORT");
builder.Configuration["Smtp:Username"] = Environment.GetEnvironmentVariable("SMTP_USER");
builder.Configuration["Smtp:Password"] = Environment.GetEnvironmentVariable("SMTP_PASS");
builder.Configuration["Smtp:From"] = Environment.GetEnvironmentVariable("SMTP_FROM");


// JWT Configuration
var jwtSettings = new JwtSettings
{
    Key = Environment.GetEnvironmentVariable("JWT_KEY")!,
    Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")!,
    Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")!,
    DurationInMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_DURATION") ?? "30")
};


// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IPasswordHasher<LabSignupUser>, PasswordHasher<LabSignupUser>>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<JwtTokenService>();



// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection")),
        mysqlOptions => mysqlOptions.MigrationsAssembly("HFiles_Backend.Infrastructure")
    )
);


// JWT 
var key = Encoding.ASCII.GetBytes(jwtSettings.Key);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var app = builder.Build();


// Migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Debugging output before migration
    Console.WriteLine("MIGRATION DB_HOST: " + Environment.GetEnvironmentVariable("DB_HOST"));
    Console.WriteLine("MIGRATION DB_USER: " + Environment.GetEnvironmentVariable("DB_USER"));

    try
    {
        using (var connection = new MySqlConnector.MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")))
        {
            connection.Open();
            Console.WriteLine("✅ Migration connection successful!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("❌ Migration connection failed: " + ex.Message);
    }

    db.Database.Migrate();
}


// Middleware
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
