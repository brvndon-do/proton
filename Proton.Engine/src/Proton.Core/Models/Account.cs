namespace Proton.Engine.Core.Models;

public enum AccountType
{
    Other = 0,
    Live,
    Paper
}

public class Account
{
    public required string AccountId { get; set; }
    public string? AccountNumber { get; set; }
    public AccountType Type { get; set; }
    public decimal Currency { get; set; }
    public decimal Cash { get; set; }
    public decimal BuyingPower { get; set; }
    public decimal Equity { get; set; }
    public decimal PortfolioValue { get; set; }
    public double MarginMultiplier { get; set; }
    public bool IsTradingEnabled { get; set; }
}
