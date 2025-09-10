using System.Diagnostics.CodeAnalysis;
using Domain.Adapters.Database.Repositories;
using Infrastructure.Database.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Database
{
    [ExcludeFromCodeCoverage]
    public static class DatabaseDependency
    {
        public static void AddDatabaseModule(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("PostgresConnection") ??
                                   throw new InvalidOperationException(
                                       "Connection string 'PostgresConnection' not found.");

            var databaseModelVersion = configuration.GetSection("DatabaseModelVersion").Value;

            services.AddScoped<IPostgresDbContext>(_ =>
                new PostgresDbContext(connectionString)
            );

            services.AddScoped<IStatementRepository>(provider =>
            {
                var postgresDbContext = provider.GetRequiredService<IPostgresDbContext>();

                return databaseModelVersion switch
                {
                    "Version1" => new StatementRepositoryV1(postgresDbContext),
                    _ => throw new InvalidOperationException("Unknown database model version")
                };
            });
        }
    }
}