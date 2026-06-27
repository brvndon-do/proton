namespace Proton.Engine.Core.Models;

public enum AccountType
{
    Other = 0,
    Live,
    Paper
}

public sealed class Account
{
    public required string AccountId { get; init; }
    public string? AccountNumber { get; init; }
    public AccountType Type { get; init; }
    public decimal Currency { get; init; }
    public decimal Cash { get; init; }
    public decimal BuyingPower { get; init; }
    public decimal Equity { get; init; }
    public decimal PortfolioValue { get; init; }
    public double MarginMultiplier { get; init; }
    public bool IsTradingEnabled { get; init; }
}
