namespace Domain.Entities;

public class TransactionEntity
{
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string? Type { get; set; }
    public string? RetrievalReferenceNumber { get; set; }
    public ProductEntity? Product { get; set; }
}
