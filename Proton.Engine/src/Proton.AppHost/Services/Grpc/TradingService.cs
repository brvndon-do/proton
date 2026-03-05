using Grpc.Core;
using Proton.Engine.AppHost.Grpc;
using Proton.Engine.AppHost.Utilities;
using Proton.Engine.Core.Services;

using ProtonTradingModels = Proton.Engine.Core.Models.Trading;

namespace Proton.Engine.AppHost.Services.Grpc;

// TODO: incorporate proper logging
public class TradingService(TradeExecutionService tradeExecutionService, ILogger<TradingService> logger) : Trading.TradingBase
{
    private readonly TradeExecutionService _tradeExecutionService = tradeExecutionService;
    private readonly ILogger<TradingService> _logger = logger;

    public override async Task<CreateOrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
    {
        if (request.Order is null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Order is required."));

        try
        {
            ProtonTradingModels.TradeOrder order = request.Order.ToCore();
            ProtonTradingModels.OrderResult result = await _tradeExecutionService.SubmitOrderAsync(order, context.CancellationToken);

            return new CreateOrderResponse { Result = result.ToGrpc() };
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<CancelOrderResponse> CancelOrder(CancelOrderRequest request, ServerCallContext context)
    {
        bool cancelled = await _tradeExecutionService.CancelOrderAsync(request.OrderId, context.CancellationToken);

        return new CancelOrderResponse { Cancelled = cancelled };
    }
}
