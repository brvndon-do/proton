using System.Globalization;
using Google.Protobuf.WellKnownTypes;

using GrpcModels = Proton.Engine.AppHost.Grpc;
using ProtonModels = Proton.Engine.Core.Models;

namespace Proton.Engine.AppHost.Utilities;

// TODO: there's going to be a lot of model mapping amongst Proton.Core's, Alpaca Market's models, and the gRPC models.
//       think about a better way to resolve this

public static class GrpcMapper
{
    public static ProtonModels.TradeOrder ToCore(this GrpcModels.TradeOrder order)
    {
        decimal quantity = ParseDecimalRequired(order.Quantity, nameof(order.Quantity));

        return new ProtonModels.TradeOrder
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

    public static GrpcModels.OrderResult ToGrpc(this ProtonModels.OrderResult result)
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

    public static GrpcModels.OrderStatus ToGrpc(this ProtonModels.OrderStatus status)
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

    private static ProtonModels.OrderSide MapOrderSide(GrpcModels.OrderSide side) => side switch
    {
        GrpcModels.OrderSide.Buy => ProtonModels.OrderSide.Buy,
        GrpcModels.OrderSide.Sell => ProtonModels.OrderSide.Sell,
        _ => ProtonModels.OrderSide.Buy
    };

    private static GrpcModels.OrderSide MapOrderSide(ProtonModels.OrderSide side) => side switch
    {
        ProtonModels.OrderSide.Buy => GrpcModels.OrderSide.Buy,
        ProtonModels.OrderSide.Sell => GrpcModels.OrderSide.Sell,
        _ => GrpcModels.OrderSide.Unspecified
    };

    private static ProtonModels.OrderType MapOrderType(GrpcModels.OrderType type) => type switch
    {
        GrpcModels.OrderType.Limit => ProtonModels.OrderType.Limit,
        GrpcModels.OrderType.Stop => ProtonModels.OrderType.Stop,
        GrpcModels.OrderType.StopLimit => ProtonModels.OrderType.StopLimit,
        _ => ProtonModels.OrderType.Market
    };

    private static ProtonModels.TimeInForce MapTimeInForce(GrpcModels.TimeInForce timeInForce) => timeInForce switch
    {
        GrpcModels.TimeInForce.Day => ProtonModels.TimeInForce.Day,
        GrpcModels.TimeInForce.Gtc => ProtonModels.TimeInForce.Gtc,
        GrpcModels.TimeInForce.Ioc => ProtonModels.TimeInForce.Ioc,
        GrpcModels.TimeInForce.Fok => ProtonModels.TimeInForce.Fok,
        _ => ProtonModels.TimeInForce.Day
    };

    private static GrpcModels.OrderState MapOrderState(ProtonModels.OrderState state) => state switch
    {
        ProtonModels.OrderState.New => GrpcModels.OrderState.New,
        ProtonModels.OrderState.PartiallyFilled => GrpcModels.OrderState.PartiallyFilled,
        ProtonModels.OrderState.Filled => GrpcModels.OrderState.Filled,
        ProtonModels.OrderState.Cancelled => GrpcModels.OrderState.Cancelled,
        ProtonModels.OrderState.Rejected => GrpcModels.OrderState.Rejected,
        _ => GrpcModels.OrderState.Unspecified
    };
}
