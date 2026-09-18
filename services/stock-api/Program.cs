using Korp.Stock.Api.Data;
using Korp.Stock.Api.Infrastructure;
using Korp.Stock.Api.Middleware;
using Korp.Stock.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Korp Estoque API", Version = "v1" });
});

builder.Services.AddDbContext<StockDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Stock")));

builder.Services.AddSingleton<ChaosState>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddSingleton<CatalogAiService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<ChaosMiddleware>();
app.UseCors("frontend");
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();
    await db.Database.EnsureCreatedAsync();
    await StockSeeder.SeedAsync(db);
}

app.Run();
