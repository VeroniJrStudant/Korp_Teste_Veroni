using Korp.Billing.Api.Data;
using Korp.Billing.Api.Domain;
using Korp.Billing.Api.Integrations;
using Korp.Billing.Api.Middleware;
using Korp.Billing.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Korp Faturamento API", Version = "v1" });
});

builder.Services.AddDbContext<BillingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Billing")));

builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<AssistantService>();

var stockBaseUrl = builder.Configuration["StockApi:BaseUrl"] ?? "http://localhost:5081";
builder.Services.AddHttpClient<IStockGateway, StockGateway>(client =>
{
    client.BaseAddress = new Uri(stockBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
}).AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(12);
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.MinimumThroughput = 4;
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5);
});

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
app.UseCors("frontend");
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    await db.Database.EnsureCreatedAsync();
    if (!db.Sequences.Any())
    {
        db.Sequences.Add(new InvoiceSequence { Id = 1, LastNumber = 0 });
        await db.SaveChangesAsync();
    }
}

app.Run();
