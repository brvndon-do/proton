# Project Name: Proton

An agentic trading engine: AI agents propose trading strategies, and a deterministic .NET engine validates them through backtesting and executes them through a broker (Alpaca).

## Core Principles

- **All .NET, one process.** No separate client and no gRPC. The user talks to the engine through an in-process REPL. `Proton.Agent/` (Rust) is legacy; don't build on it.
- **Agents never touch the broker.** Agent code submits strategies to the backtesting gate. Never inject `IOrderGateway` or `ITradeExecutionService` into it.
- **The engine is deterministic.** The same indicator library is used for backtesting and live trading, so results agree.

## Project Structure

```
Proton.Engine/
  Directory.Packages.props   # all package versions (Central Package Management)
  src/
    Proton.App/              # host entry point (Program.cs)
    Proton.Core/             # shared interfaces and models
    Proton.Repl/             # interactive REPL (Spectre.Console)
    Proton.<Module>/         # one project per module, e.g. MarketDataIngestion, Brokers.Alpaca, Database.Redis
  tests/
docs/                        # design docs; known issues in docs/engine/known_issues.md
```

## Conventions

- **Packages:** never put `Version=` on a `PackageReference`; versions go in `Directory.Packages.props`.
- **Registration:** each module exposes `AddProton*Services(...)` in its `ServiceCollectionExtensions`, returning `IServiceCollection`. `Program.cs` only chains these. Register the REPL last.
- **Options:** `sealed` class with `public const string SectionName`; bind via `AddOptions<T>().Bind(...)` and use `ValidateOnStart()` for required values. Don't read raw `configuration["..."]`.
- **Hosted services:** calling `base.StartAsync` is required when overriding `StartAsync`. Use `IEnumerable<T>` injection for multi-registrations; create scopes only for scoped services.
- **Output vs. logs:** REPL commands (`IReplCommand`) write with `AnsiConsole`. Diagnostics use `ILogger` (Serilog), which writes to `Proton.App/logs/` and only to the console when running headless.
- **Style:** namespaces follow `Proton.Engine.<Module>`; primary constructors, file-scoped namespaces, nullable enabled.

## Running

1. `docker compose up -d` from the repo root (Redis).
2. Set Alpaca keys in user-secrets on `Proton.App`: `AlpacaOptions:ApiKey`, `AlpacaOptions:ApiSecret`.
3. `dotnet build Proton.Engine/Proton.Engine.slnx`, then run `Proton.App` with `DOTNET_ENVIRONMENT=Development`. Without Development, user-secrets aren't loaded.
4. The REPL needs a real terminal. It skips itself when stdin is redirected; VS Code must use `"console": "integratedTerminal"`.

## Guidelines
- Be instructive to the user. Do NOT write any code unless asked.
- Be succinct but effective in explanations. Do NOT overload the user with a ton of information.
- Offer modern ways and patterns.
