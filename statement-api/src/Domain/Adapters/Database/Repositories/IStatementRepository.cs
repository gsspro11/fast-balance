using Domain.Entities;

namespace Domain.Adapters.Database.Repositories;

public interface IStatementRepository
{
    Task<IEnumerable<StatementEntity>> GetByIdentificationAsync(long customerIdentification,
        CancellationToken cancellationToken);

    Task<IEnumerable<StatementEntity>> GetByIdentificationAndProductAsync(long customerIdentification, int productCode,
        CancellationToken cancellationToken);

    Task<IEnumerable<StatementEntity>> GetByCardAsync(string cardNumber, CancellationToken cancellationToken);

    Task<IEnumerable<StatementEntity>> GetByCardAndProductAsync(string cardNumber, int productCode,
        CancellationToken cancellationToken);
}