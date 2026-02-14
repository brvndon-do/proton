namespace Proton.Engine.Core.Models.Trading;

public enum OrderState
{
    Unknown = 0,
    New,
    PartiallyFilled,
    Filled,
    Cancelled,
    Rejected
}
