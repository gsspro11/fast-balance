using Domain.Entities;

namespace Domain.Adapters.Database.Repositories;

public interface IBalanceRepository
{
    Task<IEnumerable<BalanceEntity>> GetByIdentificationAsync(long customerIdentification,
        CancellationToken cancellationToken);

    Task<IEnumerable<BalanceEntity>> GetByIdentificationAndProductAsync(long customerIdentification, int productCode,
        CancellationToken cancellationToken);

    Task<IEnumerable<BalanceEntity>> GetByCardAsync(string cardNumber, CancellationToken cancellationToken);

    Task<IEnumerable<BalanceEntity>> GetByCardAndProductAsync(string cardNumber, int productCode,
        CancellationToken cancellationToken);
}