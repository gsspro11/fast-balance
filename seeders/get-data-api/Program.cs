using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

const string connectionString =
    "Host=postgres;Port=5432;Database=postgres;Username=postgres;Password=S@ntos1631;Pooling=True;Minimum Pool Size=10;Maximum Pool Size=25;Connection Lifetime=600;";

const string appCacheKey = "AppCacheKey";

app.MapGet("/cards/random", async (IMemoryCache memoryCache) =>
    {
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        await using var dbConnection = new NpgsqlConnection(connectionString);
        await dbConnection.OpenAsync(cancellationToken);

        var cardNumbers = await memoryCache.GetOrCreateAsync(appCacheKey, async cacheEntry =>
        {
            var cardNumbers =
                await dbConnection.QueryAsync<long>(
                    "SELECT card_number FROM fast_balance_v1.cards ORDER BY RANDOM() LIMIT 25000");

            cacheEntry.SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

            return cardNumbers.ToArray();
        });
        
        return cardNumbers![new Random().Next(cardNumbers.Length)];
    })
    .WithName("GetRandomCard");

await app.RunAsync();