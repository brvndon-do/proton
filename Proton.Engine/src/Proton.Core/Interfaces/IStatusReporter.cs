using Proton.Engine.Core.Models;

namespace Proton.Engine.Core.Interfaces;

public interface IStatusReporter
{
    string Name { get; }
    ServiceStatus GetStatus();
}
