namespace Domain.Entities
{
    public class BalanceEntity
    {
        public required ProductEntity Product { get; set; }
        public decimal Value { get; init; }
    }
}