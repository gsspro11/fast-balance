namespace Domain.Entities;

public class BalanceEntity
{
    public decimal Value { get; set; }

    public ProductEntity? Product { get; set; }
}