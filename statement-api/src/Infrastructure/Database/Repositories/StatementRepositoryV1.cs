using System.Text.Json;
using Dapper;
using Domain.Adapters.Database.Repositories;
using Domain.Entities;

namespace Infrastructure.Database.Repositories;

public class StatementRepositoryV1(IPostgresDbContext dbContext) : IStatementRepository
{
    public async Task<IEnumerable<StatementEntity>> GetByIdentificationAsync(long customerIdentification,
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
            : JsonSerializer.Deserialize<StatementEntity[]>(resultSelect)!;
    }

    public async Task<IEnumerable<StatementEntity>> GetByIdentificationAndProductAsync(long customerIdentification,
        int productCode, CancellationToken cancellationToken)
    {
        var result = new List<StatementEntity>();
        
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
            
            var deserializedItems = JsonSerializer.Deserialize<StatementEntity[]>(item);

            if (deserializedItems != null)
            {
                result.AddRange(deserializedItems);
            }
        }

        return result.Where(x => x.Product.Code == productCode);
    }

    public async Task<IEnumerable<StatementEntity>> GetByCardAsync(string cardNumber, CancellationToken cancellationToken)
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
            : JsonSerializer.Deserialize<StatementEntity[]>(resultSelect)!;
    }

    public async Task<IEnumerable<StatementEntity>> GetByCardAndProductAsync(string cardNumber, int productCode,
        CancellationToken cancellationToken)
    {
        var result = new List<StatementEntity>();

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
            
            var deserializedItems = JsonSerializer.Deserialize<StatementEntity[]>(item);

            if (deserializedItems != null)
            {
                result.AddRange(deserializedItems);
            }
        }

        return result.Where(x => x.Product.Code == productCode);
    }
}