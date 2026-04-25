# Proton

An AI-powered algorithmic trading system — a multi-agent AI cluster generates and iterates on trading strategies, while a deterministic engine validates and executes them.

## Architecture

Proton is split into two primary domains:

- **Agentic Layer**: A multi-agent cluster consisting of a Researcher, Strategist, and Critic that collaborate to produce structured trading strategies. The agents iterate internally until the Critic approves a proposal, which is then submitted to the engine as a JSON contract via gRPC
- **Trading Engine**: The deterministic core and "source of truth." It ingests live market data, validates strategy proposals through backtesting, monitors the market for trade signals, and executes orders through broker APIs

More information can be found in the [design documentation](./docs/design_documentation.md).

## Debugging

For VS Code users, refer to `.vscode/launch.json` for C# debugging configurations. Rust debugging is needed to be added.

For Zed users, refer to `.zed/debug.json` for C# debugging configurations. Rust debugging is natively supported in Zed.

Ensure that Docker is running: `docker compose up -d`

## Core Components

### Market Ingestion

A background daemon maintains a persistent WebSocket connection to the broker, streaming real-time OHLCV bars and indicators. Data is distributed internally via `System.Threading.Channels` and externally to the Agentic Layer via gRPC streaming.

### Strategy Validation (Backtester)

When a strategy proposal arrives from the agents, the engine runs an automated backtest against locally cached Parquet data. **Walk-Forward Analysis** (in-sample vs. out-of-sample splits) is used to prevent overfitting, ensuring agents can't simply "game" the backtester with curve-fitted strategies.

### Execution Pipeline

Strategies that pass validation enter the live execution flow:

1. **Strategy Watcher**: A stateful monitor that watches the live market stream for the strategy's technical triggers
2. **Signal Generation**: When entry/exit rules are met, a signal is emitted
3. **Risk Circuit Breaker**: Validates position sizing, global drawdown limits, and account equity before allowing execution
4. **Order Manager**: Executes via the `IBroker` interface

### Feedback Loop

Failed trades and rejected backtests generate **Post-Mortem** reports stored in PostgreSQL. This context is fed back into the Agentic Layer so future strategy iterations learn from past outcomes.

## Key Design Decisions

- **Separation of Concerns**: The AI agents never touch the broker directly. All execution flows through the deterministic C# engine
- **Anti-Overfitting**: Walk-forward analysis ensures strategies are validated on unseen data, not just optimized on historical noise.
- **Idempotent Indicators**: The same technical-analysis library (Skender) is used in both backtesting and live execution to prevent indicator drift
- **Risk-First**: A circuit breaker sits between signal generation and order execution, enforceable via an admin kill switch over gRPC (this might change..)
- **Strategy TTL**: Pending strategies expire to prevent accumulation of stale watchers
- **Concurrency Safety**: `SemaphoreSlim` and `Channels` manage simultaneous backtesting and live data processing without deadlocks
