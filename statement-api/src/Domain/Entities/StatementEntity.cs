namespace Domain.Entities;

public class StatementEntity
{
    private string? _type;
    private string? _description;
    private string? _retrievalReferenceNumber;

    public string? Description
    {
        get => _description;
        set => _description = value ?? string.Empty;
    }

    public DateTime Date { get; set; }

    public decimal Value { get; set; }

    public string? Type
    {
        get => _type;
        set => _type = value ?? string.Empty;
    }

    public string? RetrievalReferenceNumber
    {
        get => _retrievalReferenceNumber;
        set => _retrievalReferenceNumber = value ?? string.Empty;
    }

    public required ProductEntity Product { get; set; }
}