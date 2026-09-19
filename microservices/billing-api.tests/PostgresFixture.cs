using Korp.Billing.Api.Data;
using Korp.Billing.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Korp.Billing.Api.Tests;

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

    public BillingDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new BillingDbContext(options);
    }

    public async Task<BillingDbContext> OpenCleanDbAsync()
    {
        var db = CreateDb();
        await db.Database.EnsureCreatedAsync();
        await db.Items.ExecuteDeleteAsync();
        await db.Invoices.ExecuteDeleteAsync();
        await db.Sequences.ExecuteDeleteAsync();
        db.Sequences.Add(new InvoiceSequence { Id = 1, LastNumber = 0 });
        await db.SaveChangesAsync();
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
            var admin = "Host=127.0.0.1;Port=5433;Username=korp;Password=korp;Database=korp_billing";
            await using var connection = new NpgsqlConnection(admin);
            await connection.OpenAsync();
            await using var exists = connection.CreateCommand();
            exists.CommandText = "SELECT 1 FROM pg_database WHERE datname = 'korp_billing_tests'";
            if (await exists.ExecuteScalarAsync() is null)
            {
                await using var create = connection.CreateCommand();
                create.CommandText = "CREATE DATABASE korp_billing_tests";
                await create.ExecuteNonQueryAsync();
            }

            ConnectionString = "Host=127.0.0.1;Port=5433;Username=korp;Password=korp;Database=korp_billing_tests";
            return true;
        }
        catch
        {
            return false;
        }
    }
}

[CollectionDefinition(nameof(BillingPostgresCollection))]
public sealed class BillingPostgresCollection : ICollectionFixture<PostgresFixture>;
