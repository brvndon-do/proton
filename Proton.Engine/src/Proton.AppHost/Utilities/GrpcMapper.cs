using System.Globalization;
using Google.Protobuf.WellKnownTypes;

using GrpcModels = Proton.Engine.AppHost.Grpc;
using ProtonTradingModels = Proton.Engine.Core.Models.Trading;

namespace Proton.Engine.AppHost.Utilities;

// TODO: there's going to be a lot of model mapping amongst Proton.Core's, Alpaca Market's models, and the gRPC models.
//       think about a better way to resolve this

public static class GrpcMapper
{
    public static ProtonTradingModels.TradeOrder ToCore(this GrpcModels.TradeOrder order)
    {
        decimal quantity = ParseDecimalRequired(order.Quantity, nameof(order.Quantity));

        return new ProtonTradingModels.TradeOrder
        {
            Symbol = order.Symbol,
            Side = MapOrderSide(order.Side),
            Quantity = quantity,
            OrderType = MapOrderType(order.OrderType),
            TimeInForce = MapTimeInForce(order.TimeInForce),
            LimitPrice = ParseDecimalOptional(order.LimitPrice),
            StopPrice = ParseDecimalOptional(order.StopPrice),
            ClientOrderId = !string.IsNullOrWhiteSpace(order.ClientOrderId) ? order.ClientOrderId : null
        };
    }

    public static GrpcModels.OrderResult ToGrpc(this ProtonTradingModels.OrderResult result)
    {
        GrpcModels.OrderResult grpcResult = new GrpcModels.OrderResult
        {
            OrderId = result.OrderId,
            Symbol = result.Symbol,
            Side = MapOrderSide(result.Side),
            Quantity = FormatDecimal(result.Quantity),
            FilledQuantity = FormatDecimal(result.FilledQuantity),
            AverageFillPrice = FormatDecimal(result.AverageFillPrice),
            Message = result.Message ?? string.Empty
        };

        if (result.SubmittedAt != default)
            grpcResult.SubmittedAt = Timestamp.FromDateTime(result.SubmittedAt.UtcDateTime);

        if (result.Status is not null)
            grpcResult.Status = result.Status.ToGrpc();

        if (result.Errors is not null)
            grpcResult.Errors.Add(result.Errors);

        return grpcResult;
    }

    public static GrpcModels.OrderStatus ToGrpc(this ProtonTradingModels.OrderStatus status)
    {
        GrpcModels.OrderStatus grpcStatus = new GrpcModels.OrderStatus
        {
            OrderId = status.OrderId,
            State = MapOrderState(status.State),
            FilledQuantity = FormatDecimal(status.FilledQuantity),
            RemainingQuantity = FormatDecimal(status.RemainingQuantity),
            Reason = status.Reason ?? string.Empty
        };

        if (status.UpdatedAt != default)
            grpcStatus.UpdatedAt = Timestamp.FromDateTime(status.UpdatedAt.UtcDateTime);

        return grpcStatus;
    }

    private static string FormatDecimal(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    private static string FormatDecimal(decimal? value) => value is not null
        ? value.Value.ToString(CultureInfo.InvariantCulture)
        : string.Empty;

    private static decimal ParseDecimalRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{fieldName} is required.");

        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
            throw new InvalidOperationException($"{fieldName} must be a valid decimal.");

        return parsed;
    }

    private static decimal? ParseDecimalOptional(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed))
            throw new InvalidOperationException("Invalid decimal value.");

        return parsed;
    }

    private static ProtonTradingModels.OrderSide MapOrderSide(GrpcModels.OrderSide side) => side switch
    {
        GrpcModels.OrderSide.Buy => ProtonTradingModels.OrderSide.Buy,
        GrpcModels.OrderSide.Sell => ProtonTradingModels.OrderSide.Sell,
        _ => ProtonTradingModels.OrderSide.Buy
    };

    private static GrpcModels.OrderSide MapOrderSide(ProtonTradingModels.OrderSide side) => side switch
    {
        ProtonTradingModels.OrderSide.Buy => GrpcModels.OrderSide.Buy,
        ProtonTradingModels.OrderSide.Sell => GrpcModels.OrderSide.Sell,
        _ => GrpcModels.OrderSide.Unspecified
    };

    private static ProtonTradingModels.OrderType MapOrderType(GrpcModels.OrderType type) => type switch
    {
        GrpcModels.OrderType.Limit => ProtonTradingModels.OrderType.Limit,
        GrpcModels.OrderType.Stop => ProtonTradingModels.OrderType.Stop,
        GrpcModels.OrderType.StopLimit => ProtonTradingModels.OrderType.StopLimit,
        _ => ProtonTradingModels.OrderType.Market
    };

    private static ProtonTradingModels.TimeInForce MapTimeInForce(GrpcModels.TimeInForce timeInForce) => timeInForce switch
    {
        GrpcModels.TimeInForce.Day => ProtonTradingModels.TimeInForce.Day,
        GrpcModels.TimeInForce.Gtc => ProtonTradingModels.TimeInForce.Gtc,
        GrpcModels.TimeInForce.Ioc => ProtonTradingModels.TimeInForce.Ioc,
        GrpcModels.TimeInForce.Fok => ProtonTradingModels.TimeInForce.Fok,
        _ => ProtonTradingModels.TimeInForce.Day
    };

    private static GrpcModels.OrderState MapOrderState(ProtonTradingModels.OrderState state) => state switch
    {
        ProtonTradingModels.OrderState.New => GrpcModels.OrderState.New,
        ProtonTradingModels.OrderState.PartiallyFilled => GrpcModels.OrderState.PartiallyFilled,
        ProtonTradingModels.OrderState.Filled => GrpcModels.OrderState.Filled,
        ProtonTradingModels.OrderState.Cancelled => GrpcModels.OrderState.Cancelled,
        ProtonTradingModels.OrderState.Rejected => GrpcModels.OrderState.Rejected,
        _ => GrpcModels.OrderState.Unspecified
    };
}
