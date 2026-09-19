using Korp.Stock.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Korp.Stock.Api.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public bool Available { get; private set; }

    public string ConnectionString { get; private set; } = "";

    public async Task InitializeAsync()
    {
        if (await TryStartTestcontainersAsync())
        {
            Available = true;
            return;
        }

        if (await TryLocalComposeAsync())
        {
            Available = true;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public StockDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<StockDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new StockDbContext(options);
    }

    public async Task<StockDbContext> OpenCleanDbAsync()
    {
        var db = CreateDb();
        await db.Database.EnsureCreatedAsync();
        await db.Database.ExecuteSqlRawAsync(
            """TRUNCATE TABLE stock_movements, products RESTART IDENTITY CASCADE""");
        return db;
    }

    private async Task<bool> TryStartTestcontainersAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
            return true;
        }
        catch
        {
            _container = null;
            return false;
        }
    }

    private async Task<bool> TryLocalComposeAsync()
    {
        try
        {
            var admin = "Host=127.0.0.1;Port=5433;Username=korp;Password=korp;Database=korp_stock";
            await using var connection = new NpgsqlConnection(admin);
            await connection.OpenAsync();
            await using var exists = connection.CreateCommand();
            exists.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'korp_stock_tests'";
            if (await exists.ExecuteScalarAsync() is null)
            {
                await using var create = connection.CreateCommand();
                create.CommandText = "CREATE DATABASE korp_stock_tests";
                await create.ExecuteNonQueryAsync();
            }

            ConnectionString = "Host=127.0.0.1;Port=5433;Username=korp;Password=korp;Database=korp_stock_tests";
            return true;
        }
        catch
        {
            return false;
        }
    }
}

[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;
