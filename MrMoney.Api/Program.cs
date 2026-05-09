var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MrMoney.Api v1");
    options.RoutePrefix = string.Empty;
});

// Enable CORS
app.UseCors("AllowAll");

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsProduction())
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    app.Urls.Add($"http://0.0.0.0:{port}");
}

app.Run();