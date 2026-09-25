# Known Issues

Open issues and deliberate workarounds in `Proton.Engine`. Remove entries as they are resolved.

## Functional gaps

- **Backtesting is a stub.** `Proton.Backtesting/BacktestingService.cs` is empty and isn't registered in `Proton.App`. It is the validation gate for every agent-proposed strategy, so it's the next priority.
- **No tests.** `Proton.Core.Tests` and `Proton.Backtesting.Tests` are scaffolds; `dotnet test` discovers zero tests.
- **Hard-coded warmup symbols.** `MarketDataIngestion.ExecuteAsync` pins AAPL, TSLA, NVDA and META. These should come from configuration or another source.

## Bugs

- **False error on shutdown during warmup.** If the app stops while a symbol's backfill is still running, the host disposes the broker connection underneath it and `MarketDataSubscriptionManager` logs:

  ```
  [ERR] Failed to backfill/subscribe to symbol to AAPL
  System.ObjectDisposedException: ... 'Connection is being disposed'.
  ```

  It depends on timing. Fix: add an exception filter to that catch that logs at Debug (or not at all) when `_cts.IsCancellationRequested`. Also check that `BackfillIfNeededAsync` honors its token between Alpaca calls.

- **Redis outage at startup is fatal.** `ConnectionMultiplexer` defaults to `AbortOnConnectFail = true`, so if Redis is down or restarting, the host fails to start. Consider `AbortOnConnectFail = false` so it retries in the background.

## Dependencies

- **Parquet.Net held at 5.5.0.** 6.x removes `ParquetSerializerOptions` and changes deserialization to return `DeserializationResult<T>`, so `ParquetRepository` won't compile. Migrate it together with the planned intraday-granularity rewrite.
- **Snappier pinned to 1.3.1.** Parquet.Net 5.5.0 pulls Snappier 1.3.0 transitively, which has a high-severity advisory (GHSA-pggp-6c3x-2xmx). It's pinned via central transitive pinning in `Directory.Packages.props`; drop the pin once Parquet.Net is upgraded.
- **StackExchange.Redis 3.x.** Upgraded from 2.12.14. Connection is verified; the read/write paths in `RedisRepository` haven't been exercised against live data yet.

## Cleanup

- **`AllowMissingPrunePackageData`** in `Proton.App.csproj` is a leftover from the old Web SDK project and may be removable.
- **`Proton.Agent/`** (Rust CLI) is legacy and pending deletion.
