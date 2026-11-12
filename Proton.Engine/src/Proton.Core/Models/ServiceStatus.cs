namespace Proton.Engine.Core.Models;

public enum ServiceState
{
    Idle,
    Running,
    Stopped
}

public sealed record ServiceStatus(
    ServiceState State,
    string Message,
    DateTimeOffset LastUpdated);
