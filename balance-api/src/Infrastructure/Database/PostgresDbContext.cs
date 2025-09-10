using Npgsql;

namespace Infrastructure.Database;

public interface IPostgresDbContext
{
    Task<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken);
}

public class PostgresDbContext(string connectionString) : IPostgresDbContext
{
    public async Task<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}