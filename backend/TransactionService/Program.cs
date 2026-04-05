using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.DTOs;
using TransactionService.Services;
using TransactionService.Validators;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<TransactionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// HttpClient para ProductService
var productServiceBaseUrl = builder.Configuration["ProductServiceBaseUrl"] ?? "http://localhost:5001";
builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>(client =>
{
    client.BaseAddress = new Uri(productServiceBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Services
builder.Services.AddScoped<ITransactionService, TransactionService.Services.TransactionService>();

// Validators
builder.Services.AddScoped<IValidator<CreateTransactionDto>, CreateTransactionValidator>();
builder.Services.AddScoped<IValidator<UpdateTransactionDto>, UpdateTransactionValidator>();

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Transaction Service API", Version = "v1" });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transaction Service API v1");
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
