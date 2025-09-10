using Api.BalanceServices;
using Domain.Adapters.Database.Repositories;
using Google.Protobuf.Collections;
using Grpc.Core;
using Mapster;

namespace Api.Services;

public class BalanceService(IBalanceRepository balanceRepository) : FastBalance.FastBalanceBase
{
    public override async Task<BalanceResponse> GetByIdentificationAsync(GetByIdentificationRequest request,
        ServerCallContext context)
    {
        var balance =
            await balanceRepository.GetByIdentificationAsync(request.CustomerIdentification, context.CancellationToken);

        return new BalanceResponse
        {
            Balances = { balance.Adapt<RepeatedField<Balance>>() }
        };
    }

    public override async Task<BalanceResponse> GetByIdentificationAndProductAsync(
        GetByIdentificationAndProductRequest request, ServerCallContext context)
    {
        var balance = await balanceRepository.GetByIdentificationAndProductAsync(request.CustomerIdentification,
            request.ProductCode, context.CancellationToken);

        return new BalanceResponse
        {
            Balances = { balance.Adapt<RepeatedField<Balance>>() }
        };
    }

    public override async Task<BalanceResponse> GetByCardAsync(GetByCardRequest request, ServerCallContext context)
    {
        var balance = await balanceRepository.GetByCardAsync(request.CardNumber, context.CancellationToken);

        return new BalanceResponse
        {
            Balances = { balance.Adapt<RepeatedField<Balance>>() }
        };
    }

    public override async Task<BalanceResponse> GetByCardAndProductAsync(GetByCardAndProductRequest request,
        ServerCallContext context)
    {
        var balance =
            await balanceRepository.GetByCardAndProductAsync(request.CardNumber, request.ProductCode,
                context.CancellationToken);

        return new BalanceResponse
        {
            Balances = { balance.Adapt<RepeatedField<Balance>>() }
        };
    }
}