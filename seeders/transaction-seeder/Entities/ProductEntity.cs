namespace Domain.Entities;

public class ProductEntity
{
    public required string Description { get; set; }
    public int Code { get; set; }
    private string? _type;
    public string? Type 
    {
        get => _type;
        set => _type = value ?? string.Empty;
    }
}