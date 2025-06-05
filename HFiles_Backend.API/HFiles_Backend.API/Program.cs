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
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Load .env file explicitly
DotNetEnv.Env.Load();

// Bind environment variables into Configuration system
builder.Configuration["ConnectionStrings:DefaultConnection"] = $"Server={Environment.GetEnvironmentVariable("DB_HOST")};" +
                                                             $"Port={Environment.GetEnvironmentVariable("DB_PORT")};" +
                                                             $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                                                             $"User={Environment.GetEnvironmentVariable("DB_USER")};" +
                                                             $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};";

builder.Configuration["Smtp:Host"] = Environment.GetEnvironmentVariable("SMTP_HOST");
builder.Configuration["Smtp:Port"] = Environment.GetEnvironmentVariable("SMTP_PORT");
builder.Configuration["Smtp:Username"] = Environment.GetEnvironmentVariable("SMTP_USER");
builder.Configuration["Smtp:Password"] = Environment.GetEnvironmentVariable("SMTP_PASS");
builder.Configuration["Smtp:From"] = Environment.GetEnvironmentVariable("SMTP_FROM");

builder.Configuration["Interakt:ApiUrl"] = Environment.GetEnvironmentVariable("INTERAKT_API_URL");
builder.Configuration["Interakt:ApiKey"] = Environment.GetEnvironmentVariable("INTERAKT_API_KEY");

// Add JWT section to Configuration explicitly for binding
builder.Configuration["JwtSettings:Key"] = Environment.GetEnvironmentVariable("JWT_KEY") ?? throw new Exception("JWT_KEY missing");
builder.Configuration["JwtSettings:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? throw new Exception("JWT_ISSUER missing");
builder.Configuration["JwtSettings:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? throw new Exception("JWT_AUDIENCE missing");
builder.Configuration["JwtSettings:DurationInMinutes"] = Environment.GetEnvironmentVariable("JWT_DURATION") ?? "30";

// Log loaded variables
Console.WriteLine("DB Connection String: " + builder.Configuration.GetConnectionString("DefaultConnection"));
Console.WriteLine("JWT Key Present: " + (!string.IsNullOrEmpty(builder.Configuration["JwtSettings:Key"])));

try
{
    using var connection = new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
    connection.Open();
    Console.WriteLine("✅ Database connection successful!");
}
catch (Exception ex)
{
    Console.WriteLine("❌ Connection failed: " + ex.Message);
}

// Bind JwtSettings from configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Enable session storage
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

// CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Services & Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "HFiles API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token like this: Bearer {your token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Scoped services and JwtTokenService
builder.Services.AddScoped<IPasswordHasher<LabSignupUser>, PasswordHasher<LabSignupUser>>();
builder.Services.AddScoped<IPasswordHasher<LabAdmin>, PasswordHasher<LabAdmin>>();
builder.Services.AddScoped<IPasswordHasher<LabMember>, PasswordHasher<LabMember>>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.Configure<WhatsappSettings>(builder.Configuration.GetSection("Interakt"));
builder.Services.AddHttpClient<IWhatsappService, WhatsappService>();
builder.Services.AddScoped<LabAuthorizationService>();


// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection")),
        mysqlOptions => mysqlOptions.MigrationsAssembly("HFiles_Backend.Infrastructure")
    )
);

// Setup JWT Authentication
// Get JwtSettings from DI 
var tempJwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
                      ?? throw new Exception("Failed to load JwtSettings from configuration");

if (string.IsNullOrEmpty(tempJwtSettings.Key))
{
    throw new Exception("JWT secret key (JwtSettings.Key) is missing or empty");
}

var key = Encoding.ASCII.GetBytes(tempJwtSettings.Key);

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
        ValidIssuer = tempJwtSettings.Issuer,
        ValidAudience = tempJwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

var app = builder.Build();

// Run migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    Console.WriteLine("MIGRATION DB_HOST: " + Environment.GetEnvironmentVariable("DB_HOST"));
    Console.WriteLine("MIGRATION DB_USER: " + Environment.GetEnvironmentVariable("DB_USER"));

    try
    {
        using var connection = new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
        connection.Open();
        Console.WriteLine("✅ Migration connection successful!");
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

app.UseSession();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Static files
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads")),
    RequestPath = "/uploads"
});

app.MapControllers();
app.Run();
