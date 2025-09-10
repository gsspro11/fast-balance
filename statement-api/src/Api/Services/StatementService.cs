using Api.StatementServices;
using Domain.Adapters.Database.Repositories;
using Google.Protobuf.Collections;
using Grpc.Core;
using Mapster;

namespace Api.Services;

public class StatementService(IStatementRepository statementRepository) : FastStatement.FastStatementBase
{
    public override async Task<StatementResponse> GetByIdentificationAsync(GetByIdentificationRequest request,
        ServerCallContext context)
    {
        var statement =
            await statementRepository.GetByIdentificationAsync(request.CustomerIdentification, context.CancellationToken);

        return new StatementResponse
        {
            Statements = { statement.Adapt<RepeatedField<Statement>>() }
        };
    }

    public override async Task<StatementResponse> GetByIdentificationAndProductAsync(
        GetByIdentificationAndProductRequest request, ServerCallContext context)
    {
        var statement = await statementRepository.GetByIdentificationAndProductAsync(request.CustomerIdentification,
            request.ProductCode, context.CancellationToken);

        return new StatementResponse
        {
            Statements = { statement.Adapt<RepeatedField<Statement>>() }
        };
    }

    public override async Task<StatementResponse> GetByCardAsync(GetByCardRequest request, ServerCallContext context)
    {
        var statement = await statementRepository.GetByCardAsync(request.CardNumber, context.CancellationToken);

        return new StatementResponse
        {
            Statements = { statement.Adapt<RepeatedField<Statement>>() }
        };
    }

    public override async Task<StatementResponse> GetByCardAndProductAsync(GetByCardAndProductRequest request,
        ServerCallContext context)
    {
        var statement =
            await statementRepository.GetByCardAndProductAsync(request.CardNumber, request.ProductCode,
                context.CancellationToken);

        return new StatementResponse
        {
            Statements = { statement.Adapt<RepeatedField<Statement>>() }
        };
    }
}