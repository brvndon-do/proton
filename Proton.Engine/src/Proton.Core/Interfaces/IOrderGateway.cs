using Proton.Engine.Core.Models.Trading;

namespace Proton.Engine.Core.Interfaces;

public interface IOrderGateway
{
    Task<OrderResult> CreateOrderAsync(TradeOrder order, CancellationToken cancellationToken = default);
    Task<bool> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default);
}
