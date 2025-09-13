using LoanTrackerJLC.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LoanTrackerJLCDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalDev", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options => {
        // JWT config
    });

builder.Services.AddAuthorization();

// ✅ Do NOT hardcode port. Azure sets PORT environment variable.
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrEmpty(port))
    {
        options.Listen(System.Net.IPAddress.Any, int.Parse(port));
    }
    else
    {
        options.Listen(System.Net.IPAddress.Any, 5000); // fallback for local dev
    }
});

var app = builder.Build();

// ✅ Enable Swagger ALWAYS in Production (or based on config)
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowLocalDev");

// ✅ Keep HTTPS redirection in production
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
