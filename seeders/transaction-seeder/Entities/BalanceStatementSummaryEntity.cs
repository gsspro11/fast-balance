using System.Text.Json;

namespace Domain.Entities;

public class BalanceStatementSummaryEntity
{
    public string? CardNumber { get; set; }
    public string? CustomerIdentification { get; set; }
    public List<BalanceEntity>? Balances { get; set; }
    public List<TransactionEntity>? Statements { get; set; }
    public string? JsonBalances => JsonSerializer.Serialize(Balances);
    public string? JsonStatements => JsonSerializer.Serialize(Statements);
}