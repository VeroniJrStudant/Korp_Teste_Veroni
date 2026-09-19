using Korp.Stock.Api.Data;
using Korp.Stock.Api.Domain;

namespace Korp.Stock.Api.Data;

public static class StockSeeder
{
    public static async Task SeedAsync(StockDbContext db)
    {
        if (db.Products.Any())
        {
            return;
        }

        db.Products.AddRange(
            new Product { Code = "ACO-1020", Description = "Barra de aço SAE 1020 Ø 1\" x 6m", Balance = 10 },
            new Product { Code = "PAR-M8", Description = "Parafuso sextavado M8 x 20 mm", Balance = 1 },
            new Product { Code = "CHP-304", Description = "Chapa inox 304 2mm 1200x3000", Balance = 25 },
            new Product { Code = "DSC-45", Description = "Disco de corte 4.1/2\" inox", Balance = 50 },
            new Product { Code = "OLE-20L", Description = "Óleo lubrificante industrial 20L", Balance = 8 }
        );

        await db.SaveChangesAsync();
    }
}
