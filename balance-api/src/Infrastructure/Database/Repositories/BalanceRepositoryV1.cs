using System.Text.Json;
using Dapper;
using Domain.Adapters.Database.Repositories;
using Domain.Entities;

namespace Infrastructure.Database.Repositories;

public class BalanceRepositoryV1(IPostgresDbContext dbContext) : IBalanceRepository
{
    public async Task<IEnumerable<BalanceEntity>> GetByIdentificationAsync(long customerIdentification,
        CancellationToken cancellationToken)
    {
        const string query = """
                             SELECT balances
                               FROM fast_balance_v1.balance_statement_summaries
                              WHERE card_number = @CardNumber
                             """;

        await using var conn = await dbContext.CreateConnectionAsync(cancellationToken);
        var resultSelect = await conn.QueryFirstOrDefaultAsync<string>(query,
            new { customerIdentification = customerIdentification.ToString() });

        return string.IsNullOrEmpty(resultSelect)
            ? []
            : JsonSerializer.Deserialize<BalanceEntity[]>(resultSelect)!;
    }

    public async Task<IEnumerable<BalanceEntity>> GetByIdentificationAndProductAsync(long customerIdentification,
        int productCode, CancellationToken cancellationToken)
    {
        var result = new List<BalanceEntity>();

        const string query = """
                             SELECT balances
                               FROM fast_balance_v1.balance_statement_summaries
                              WHERE customer_identification = @CustomerIdentification
                             """;

        await using var conn = await dbContext.CreateConnectionAsync(cancellationToken);
        var resultSelect =
            await conn.QueryAsync<string>(query, new { customerIdentification = customerIdentification.ToString() });

        foreach (var item in resultSelect)
        {
            if (string.IsNullOrEmpty(item)) continue;

            var deserializedItems = JsonSerializer.Deserialize<BalanceEntity[]>(item);

            if (deserializedItems != null)
            {
                result.AddRange(deserializedItems);
            }
        }

        return result.Where(x => x.Product?.Code == productCode);
    }

    public async Task<IEnumerable<BalanceEntity>> GetByCardAsync(string cardNumber, CancellationToken cancellationToken)
    {
        const string query = """
                             SELECT balances
                               FROM fast_balance_v1.balance_statement_summaries
                              WHERE card_number = @CardNumber
                             """;

        await using var conn = await dbContext.CreateConnectionAsync(cancellationToken);
        var resultSelect = await conn.QueryFirstOrDefaultAsync<string>(query, new { cardNumber });

        return string.IsNullOrEmpty(resultSelect)
            ? []
            : JsonSerializer.Deserialize<BalanceEntity[]>(resultSelect)!;
    }

    public async Task<IEnumerable<BalanceEntity>> GetByCardAndProductAsync(string cardNumber, int productCode,
        CancellationToken cancellationToken)
    {
        var result = new List<BalanceEntity>();

        const string query = """
                             SELECT balances
                              FROM fast_balance_v1.balance_statement_summaries
                             WHERE card_number = @CardNumber
                             """;

        await using var conn = await dbContext.CreateConnectionAsync(cancellationToken);
        var resultSelect =
            await conn.QueryAsync<string>(query, new { cardNumber });

        foreach (var item in resultSelect)
        {
            if (string.IsNullOrEmpty(item)) continue;

            var deserializedItems = JsonSerializer.Deserialize<BalanceEntity[]>(item);

            if (deserializedItems != null)
            {
                result.AddRange(deserializedItems);
            }
        }

        return result.Where(x => x.Product?.Code == productCode);
    }
}