using System.Diagnostics;
using Dapper;
using Domain.Entities;
using Npgsql;

namespace TransactionSeeder
{
    public class Transaction
    {
        public Guid NotificationId { get; init; }
        public long AccountId { get; init; }
        public long CardId { get; init; }
        public long OperationId { get; init; }
        public int ProductId { get; init; }
        public DateTime TransactionDate { get; init; }
        public decimal Amount { get; init; }
        public decimal? BalanceAfterTransaction { get; init; }
        public string? Description { get; init; }
        public long? Nsu { get; init; }
        public DateTime CreatedDate { get; init; }
    }

    internal static class Program
    {
        private const string ConnectionString =
            "Host=postgres;Port=5432;Database=postgres;Username=postgres;Password=S@ntos1631;Pooling=True;Minimum Pool Size=10;Maximum Pool Size=25;Connection Lifetime=600;";

        private static async Task Main()
        {
            using var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            const int totalTransactions = 1500; // Adjust batch size for testing
            const int batchSize = 100; // Number of transactions per batch
            var random = new Random();

            await using var dbConnection = new NpgsqlConnection(ConnectionString);
            await dbConnection.OpenAsync(cancellationToken);

            // Cache foreign key IDs in memory to avoid redundant queries
            var cards =
                (await dbConnection.QueryAsync<(long, string)>(
                    "SELECT id, card_number FROM fast_balance_v1.cards")).ToArray();

            var operationIds =
                (await dbConnection.QueryAsync<long>("SELECT id FROM fast_balance_v1.transaction_operations"))
                .ToArray();

            var productIds = (await dbConnection.QueryAsync<int>("SELECT id FROM fast_balance_v1.products")).ToArray();
            var accounts =
                (await dbConnection.QueryAsync<(long, string)>(
                    "SELECT id, customer_identification FROM fast_balance_v1.accounts")).ToArray();

            while (!cancellationToken.IsCancellationRequested)
            {
                // Use stopwatch to monitor performance
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                // Generate and insert transactions in parallel
                const int totalBatches = totalTransactions / batchSize;
                for (var batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    var transactions =
                        GenerateTransactions(accounts, cards, operationIds, productIds, random, batchSize).ToList();
                    await BulkInsertTransactionsAsync(dbConnection, transactions);
                    await UpdateSummaries(dbConnection, accounts, cards, transactions);
                }

                stopwatch.Stop();
                Console.WriteLine(
                    $"All transactions inserted! Total elapsed time: {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
            }
        }

        private static async Task UpdateSummaries(NpgsqlConnection dbConnection, (long, string)[] accounts,
            (long, string)[] cards, IEnumerable<Transaction> transactions)
        {
            var enumerable = transactions as Transaction[] ?? transactions.ToArray();
            var accountAndCardIds = enumerable.Select(t => new { t.AccountId, t.CardId }).Distinct().ToArray();
            var listBalanceStatementSummary = new List<BalanceStatementSummaryEntity>();

            foreach (var accountAndCardId in accountAndCardIds)
            {
                var lastBalances = (await dbConnection.QueryAsync<BalanceEntity, ProductEntity, BalanceEntity>("""
                        SELECT a.balance/100::DECIMAL(10, 2) AS value,
                               p.id AS code,
                               p.description AS description,
                               p.abbreviation AS type
                          FROM fast_balance_v1.accounts a
                          JOIN fast_balance_v1.products p ON a.product_id = p.id
                         WHERE a.id = @accountId 
                        """,
                    (balance, product) =>
                    {
                        balance.Product = product;
                        return balance;
                    },
                    new { accountAndCardId.AccountId },
                    splitOn: "code")).ToList();

                var lastTransactions =
                    (await dbConnection.QueryAsync<TransactionEntity, ProductEntity, TransactionEntity>("""
                            SELECT t.description AS description, 
                                   t.transaction_date AS date, 
                                   t.amount/100::DECIMAL(10, 2) AS value, 
                                   o.operation_name AS type, 
                                   p.id AS code,
                                   p.description AS description,
                                   p.abbreviation AS type
                              FROM fast_balance_v1.transactions t
                              JOIN fast_balance_v1.transaction_operations o ON t.operation_id = o.id
                              JOIN fast_balance_v1.products p ON t.product_id = p.id
                             WHERE account_id = @accountId 
                               AND transaction_date >= CURRENT_DATE - INT '90'
                            ORDER BY transaction_date DESC;
                            """,
                        (balance, product) =>
                        {
                            balance.Product = product;
                            return balance;
                        },
                        new { accountAndCardId.AccountId },
                        splitOn: "code")).ToList();

                listBalanceStatementSummary.Add(new BalanceStatementSummaryEntity
                {
                    CardNumber = cards.First(x => x.Item1.Equals(accountAndCardId.CardId)).Item2,
                    CustomerIdentification = accounts.First(x => x.Item1.Equals(accountAndCardId.AccountId)).Item2,
                    Balances = lastBalances,
                    Statements = lastTransactions
                });
            }

            // Use PostgreSQL's COPY command for bulk importing rows
            const string upsertSql = """
                                     INSERT INTO fast_balance_v1.balance_statement_summaries
                                         (card_number, customer_identification, balances, statements)
                                     VALUES (@CardNumber, @CustomerIdentification, @JsonBalances::jsonb, @JsonStatements::jsonb)
                                     ON CONFLICT(card_number)
                                     DO UPDATE SET
                                       balances = EXCLUDED.balances,
                                       statements = EXCLUDED.statements;
                                     """;

            await dbConnection.ExecuteAsync(upsertSql, listBalanceStatementSummary);
        }

        // Generates a batch of transactions
        private static IEnumerable<Transaction> GenerateTransactions(
            (long, string)[] accounts, (long, string)[] cards, long[] operationIds, int[] productIds,
            Random random, int batchSize
        )
        {
            for (var i = 0; i < batchSize; i++)
            {
                yield return new Transaction
                {
                    NotificationId = Guid.NewGuid(),
                    AccountId = accounts[random.Next(accounts.Length)].Item1, // Random account_id
                    CardId = cards[random.Next(cards.Length)].Item1, // Random card_id
                    OperationId = operationIds[random.Next(operationIds.Length)], // Random operation_id
                    ProductId = productIds[random.Next(productIds.Length)], // Random product_id
                    TransactionDate = DateTime.Now.AddDays(-random.Next(1, 365)), // Random date in the past year
                    Amount = random.Next(1, 100001), // Random amount
                    BalanceAfterTransaction = random.NextDouble() < 0.9
                        ? random.Next(1, 1000001)
                        : null,
                    Description = random.NextDouble() < 0.7
                        ? Guid.NewGuid().ToString()
                        : null,
                    Nsu = random.NextDouble() < 0.5
                        ? random.Next(1, 1000001)
                        : null,
                    CreatedDate = DateTime.Now
                };
            }
        }

        // Bulk inserts transactions using PostgreSQL COPY
        private static async Task BulkInsertTransactionsAsync(NpgsqlConnection dbConnection,
            IEnumerable<Transaction> transactions)
        {
            // Use PostgreSQL's COPY command for bulk importing rows
            await using var writer = await dbConnection.BeginTextImportAsync("""
                                                                                 COPY fast_balance_v1.transactions (
                                                                                     notification_id, account_id, card_id, operation_id, product_id, 
                                                                                     transaction_date, amount, balance_after_transaction, description, nsu, created_date
                                                                                 ) FROM STDIN (FORMAT CSV)
                                                                             """);
            // Write data to the PostgreSQL COPY stream
            foreach (var transaction in transactions)
            {
                var row = $"{transaction.NotificationId}," +
                          $"{transaction.AccountId}," +
                          $"{transaction.CardId}," +
                          $"{transaction.OperationId}," +
                          $"{transaction.ProductId}," +
                          $"{transaction.TransactionDate:yyyy-MM-dd HH:mm:ss}," +
                          $"{transaction.Amount}," +
                          $"{(transaction.BalanceAfterTransaction?.ToString() ?? "")}," +
                          $"\"{(transaction.Description ?? "")}\"," +
                          $"{(transaction.Nsu?.ToString() ?? "")}," +
                          $"{transaction.CreatedDate:yyyy-MM-dd HH:mm:ss}";

                await writer.WriteLineAsync(row);
            }
        }
    }
}