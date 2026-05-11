using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MrMoney.Api.Infrastructure;
using MrMoney.Api.Repositories;
using MrMoney.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
Env.Load("MrMoney.Api.env");
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables();

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "MrMoney API",
        Version = "v1",
        Description = "Personal finance tracker API backed by Google Sheets"
    });

    // Add JWT bearer auth to Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token (without 'Bearer ' prefix)"
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

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── JWT Authentication ────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// ── Google Sheets Infrastructure ──────────────────────────────────────────────
// Registered as Singleton — one SheetsService instance shared across all requests
builder.Services.AddSingleton<GoogleSheetsClient>();
builder.Services.AddSingleton<GoogleDriveClient>();

// ── Repositories (Scoped) ─────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository,        UserRepository>();
builder.Services.AddScoped<IAccountRepository,     AccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ICategoryRepository,    CategoryRepository>();

// ── Services (Scoped) ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IAccountService,     AccountService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICategoryService,    CategoryService>();
builder.Services.AddScoped<IUserService,        UserService>();
builder.Services.AddScoped<IDashboardService,   DashboardService>();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Ensure Google Sheets tabs exist on startup ────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var sheetsClient = scope.ServiceProvider.GetRequiredService<GoogleSheetsClient>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await sheetsClient.EnsureSheetsExistAsync();
        logger.LogInformation("Google Sheets initialized successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex,
            "Failed to initialize Google Sheets. " +
            "Verify that: (1) SpreadsheetId in appsettings.json is correct, " +
            "(2) the service account has Editor access to the spreadsheet, " +
            "(3) google-service-account.json is present in the project folder.");
        // Do NOT throw — let the app start so Swagger is accessible for debugging
    }
}

// ── Swagger ───────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MrMoney API V1");
    options.RoutePrefix = string.Empty;
});

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
