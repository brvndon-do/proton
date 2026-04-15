# Plan: Refactor Market Data Ingestion to Subscription Model

## TL;DR
Replace the current per-request socket streaming with a pub/sub subscription model where one upstream WebSocket per symbol feeds into Redis cache and Parquet storage. Clients subscribe to symbols and read from the cache — never directly from raw sockets. Historical backfill occurs automatically when a new symbol is first subscribed.

---

## Phase 1: Core Subscription Abstractions (no dependencies)

Define the new subscription contract and supporting models in `Proton.Core`.

### Steps

1. **Create `IMarketDataSubscriptionManager` interface** in `Proton.Core/Interfaces/`
   - `Task SubscribeAsync(string symbol, CancellationToken ct)` — ensures an upstream stream exists for the symbol
   - `Task UnsubscribeAsync(string symbol, CancellationToken ct)` — decrements subscriber count; tears down upstream when count reaches 0
   - `IAsyncEnumerable<Bar> GetBarStreamAsync(string symbol, CancellationToken ct)` — returns a per-subscriber broadcast stream for a symbol (backed by a `Channel<Bar>` per subscriber)
   - `IReadOnlyCollection<string> GetActiveSymbols()` — returns currently streaming symbols
   - `bool IsSymbolActive(string symbol)` — check if a symbol is already streaming

2. **Create subscription-related models** in `Proton.Core/Models/MarketData/`
   - `SymbolSubscription` — tracks per-symbol state: subscriber count, CancellationTokenSource for the upstream task, list of subscriber channels
   - Consider whether `MarketDataContext` / `MarketDataRequest` need updating or can be retired (likely retired — replaced by direct subscription calls)

3. **Expand `ICacheRepository` interface** in `Proton.Core/Interfaces/Repositories/`
   - Add `Task<IEnumerable<Bar>> GetLatestBarsAsync(string symbol, int count, CancellationToken ct)` — retrieve most recent N bars for a symbol from Redis
   - Add `Task SetLatestBarAsync(string symbol, Bar bar, CancellationToken ct)` — push a bar into the sorted set for a symbol
   - Consider key schema: `bars:{symbol}` as a Redis sorted set keyed by UTC timestamp
   - Keep the existing `IRepository<string, Bar>` base for generic CRUD

### Files to create
- `Proton.Core/Interfaces/IMarketDataSubscriptionManager.cs`
- `Proton.Core/Models/MarketData/SymbolSubscription.cs` (if needed as a shared model; may stay internal to the implementation)

### Files to modify
- `Proton.Core/Interfaces/Repositories/ICacheRepository.cs` — add symbol-specific bar retrieval methods

---

## Phase 2: Redis Cache Implementation (depends on Phase 1)

Implement the `ICacheRepository` with real Redis operations using StackExchange.Redis.

### Steps

4. **Add StackExchange.Redis NuGet package** to `Proton.Database.Redis.csproj`
   - Also add `Microsoft.Extensions.Options` for configuration injection

5. **Create `RedisOptions` configuration model** in `Proton.Database.Redis/`
   - `ConnectionString` property
   - Bind from `appsettings.json` section `"Redis"`

6. **Rewrite `RedisRepository`** to implement all `ICacheRepository` methods
   - Inject `IConnectionMultiplexer` (or `IOptions<RedisOptions>` to create one)
   - Use Redis Sorted Sets (`ZADD`, `ZRANGEBYSCORE`) for per-symbol bar storage keyed by `bars:{symbol}`, scored by UTC ticks
   - Serialize `Bar` to JSON (System.Text.Json) for values
   - `SetLatestBarAsync` → `ZADD bars:{symbol} <timestamp_score> <bar_json>`
   - `GetLatestBarsAsync` → `ZREVRANGEBYSCORE bars:{symbol}` with count limit
   - `GetByKeyAsync` → `ZREVRANGEBYSCORE bars:{symbol}` returning latest single bar
   - `AddAsync` / `AddRangeAsync` → batch `ZADD`
   - Consider TTL for cache eviction (e.g., keep only last 24h of minute bars)

7. **Register Redis in DI** — update `Program.cs`
   - Add `builder.Services.Configure<RedisOptions>(...)` binding
   - Register `IConnectionMultiplexer` as singleton from config
   - Keep existing `ICacheRepository → RedisRepository` registration

### Files to create
- `Proton.Database.Redis/RedisOptions.cs`

### Files to modify
- `Proton.Database.Redis/RedisRepository.cs` — full rewrite
- `Proton.Database.Redis/Proton.Database.Redis.csproj` — add StackExchange.Redis
- `Proton.AppHost/Program.cs` — Redis DI wiring
- `Proton.AppHost/appsettings.json` / `appsettings.Development.json` — add Redis connection config

---

## Phase 3: Subscription Manager Implementation (depends on Phase 1 & 2)

Build the `MarketDataSubscriptionManager` — the core orchestrator that manages one upstream per symbol and fans out to subscribers.

### Steps

8. **Create `MarketDataSubscriptionManager`** in `Proton.MarketDataIngestion/`
   - Implements `IMarketDataSubscriptionManager`
   - Inject `IMarketDataProvider`, `ICacheRepository`, `IBarRepository`, `ILogger`
   - Maintain `ConcurrentDictionary<string, SymbolSubscription>` for active symbols
   - Each `SymbolSubscription` holds:
     - `CancellationTokenSource` for the upstream task
     - `List<Channel<Bar>>` for subscriber broadcast channels (protected by lock or `ConcurrentBag`)
     - `int SubscriberCount` (thread-safe via Interlocked)
     - `Task UpstreamTask` reference

9. **Implement `SubscribeAsync`**
   - If symbol not active: start upstream task (see step 10), set count = 1
   - If symbol active: increment count
   - Uses `ConcurrentDictionary.GetOrAdd` + lock for atomic start

10. **Implement upstream ingestion loop** (private method, one per symbol)
    - Calls `IMarketDataProvider.StreamBarsAsync([symbol], ct)`
    - For each incoming bar:
      - Write to Redis via `ICacheRepository.SetLatestBarAsync`
      - Batch-write to Parquet via `IBarRepository.AddRangeAsync` (same batching logic as current, batch size 100)
      - Broadcast to all subscriber channels (iterate and `TryWrite` — drop if subscriber is slow)
    - On cancellation: clean up, complete all subscriber channels

11. **Implement `GetBarStreamAsync`**
    - Create a new `Channel<Bar>` for the caller
    - Register it in the symbol's subscriber list
    - Yield from `channel.Reader.ReadAllAsync(ct)`
    - On caller cancellation: unregister channel from subscriber list

12. **Implement `UnsubscribeAsync`**
    - Decrement subscriber count
    - If count reaches 0: cancel upstream CTS, remove from dictionary
    - Guard against race conditions with lock

13. **Implement historical backfill** in `SubscribeAsync` (when starting a new upstream)
    - Before starting the live stream, check if bars exist in Parquet via `IBarRepository.ReadBarsAsync(symbol)`
    - If empty: call `IMarketDataProvider.GetHistoricalBarsAsync` for the symbol with a reasonable lookback (e.g., 30 days daily, or configurable)
    - Write historical bars to both Parquet and Redis cache
    - This ensures indicator calculations have enough history from the start

### Files to create
- `Proton.MarketDataIngestion/MarketDataSubscriptionManager.cs`

### Files to modify
- `Proton.MarketDataIngestion/Proton.MarketDataIngestion.csproj` — may need to add reference to Redis project if not already present

---

## Phase 4: Rewrite MarketDataIngestion BackgroundService (depends on Phase 3)

Simplify the hosted service — it no longer orchestrates streams itself; it delegates to the subscription manager.

### Steps

14. **Rewrite `MarketDataIngestionService.cs`**
    - Remove the `ProcessMarketDataContextsAsync` method and channel-reading loop
    - The service's `ExecuteAsync` can now either:
      - (a) Be removed entirely if `MarketStarterService` handles initial subscriptions and gRPC handles client subscriptions, OR
      - (b) Serve as a health-check / monitoring loop that logs active subscriptions periodically
    - Move `ProcessMarketNewsContextsAsync` to remain channel-based (news doesn't need the subscription model since it's global, not per-symbol) OR keep it as-is in a separate service
    - Decision: Keep `MarketDataIngestion` as the BackgroundService that owns the news ingestion loop. Market data stream management moves entirely to `MarketDataSubscriptionManager`.

### Files to modify
- `Proton.MarketDataIngestion/MarketDataIngestionService.cs` — major rewrite (remove market data channel loop, keep news loop)

---

## Phase 5: Rewrite MarketStarterService (depends on Phase 3)

### Steps

15. **Rewrite `MarketStarterService`**
    - Inject `IMarketDataSubscriptionManager` instead of `IChannelManager`
    - In `ExecuteAsync`: iterate over configured symbols and call `SubscribeAsync` for each
    - Read symbols from `IConfiguration` (e.g., `"MarketData:DefaultSymbols"` array in appsettings) instead of hardcoding
    - This is the "pre-loader" — it ensures streams are active before any client connects

16. **Add default symbols to appsettings.json**
    - `"MarketData": { "DefaultSymbols": ["AAPL", "TSLA", "NVDA", "META"] }`

### Files to modify
- `Proton.AppHost/Services/Background/MarketStarterService.cs` — rewrite
- `Proton.AppHost/appsettings.json` — add MarketData config section

---

## Phase 6: Rewrite gRPC MarketDataService (depends on Phase 3)

### Steps

17. **Rewrite `StreamMarketSnapshot`**
    - Inject `IMarketDataSubscriptionManager` and `ICacheRepository`
    - On client request: call `SubscribeAsync` for each requested symbol
    - Stream bars via `GetBarStreamAsync` → convert to gRPC `MarketSnapshot` via `GrpcMapper`
    - On client disconnect (cancellation): call `UnsubscribeAsync` for each symbol
    - Use try/finally to guarantee unsubscribe on disconnect

18. **Optionally add a `GetLatestSnapshot` unary RPC** (new proto method)
    - Read latest bar(s) from Redis cache via `ICacheRepository.GetLatestBarsAsync`
    - Useful for clients that want a point-in-time snapshot without subscribing to a stream
    - *Can be deferred to a later iteration*

19. **Keep `StreamNewsSnapshot` and `GetNewsSnapshot` as-is** — news doesn't change in this refactor

### Files to modify
- `Proton.AppHost/Services/Grpc/MarketDataService.cs` — rewrite StreamMarketSnapshot
- `Proton.AppHost/Protos/market_data.proto` — optionally add GetLatestSnapshot RPC (can defer)

---

## Phase 7: Cleanup & DI Wiring (depends on all above)

### Steps

20. **Update `Program.cs`** DI registrations
    - Register `IMarketDataSubscriptionManager → MarketDataSubscriptionManager` as **singleton**
    - Ensure `MarketStarterService` and `MarketDataIngestion` hosted services are still registered
    - Remove any unused registrations

21. **Clean up deprecated models**
    - Remove or mark `MarketDataContext` as obsolete if no longer used (market data now uses subscription manager, not context channels)
    - Evaluate whether `IChannelManager.MarketDataContextChannel` can be removed (keep `MarketNewsContextChannel` for news)
    - If `MarketDataContext` is removed: simplify `IChannelManager` to only hold news channel
    - Update `ChannelManager` accordingly

22. **Update `GrpcMapper`**
    - Verify `ToGrpc()` extension methods still work with the new data flow
    - Remove `ToCore()` for `MarketSnapshotRequest` if no longer needed (clients now subscribe by symbol strings directly)

### Files to modify
- `Proton.AppHost/Program.cs`
- `Proton.Core/Interfaces/IChannelManager.cs` — remove MarketDataContextChannel if unused
- `Proton.AppHost/Managers/ChannelManager.cs` — match interface changes
- `Proton.Core/Models/MarketData/MarketDataContext.cs` — deprecate or delete
- `Proton.Core/Models/MarketData/MarketDataRequest.cs` — evaluate if still needed
- `Proton.AppHost/Utilities/GrpcMapper.cs` — remove unused mappings

---

## Relevant Files

### Create
- `Proton.Core/Interfaces/IMarketDataSubscriptionManager.cs` — new subscription contract
- `Proton.MarketDataIngestion/MarketDataSubscriptionManager.cs` — core orchestrator implementation
- `Proton.Database.Redis/RedisOptions.cs` — Redis connection config model

### Rewrite
- `Proton.Database.Redis/RedisRepository.cs` — real Redis implementation with sorted sets
- `Proton.MarketDataIngestion/MarketDataIngestionService.cs` — simplified to news-only ingestion
- `Proton.AppHost/Services/Background/MarketStarterService.cs` — config-driven subscription pre-loader
- `Proton.AppHost/Services/Grpc/MarketDataService.cs` — subscription-based streaming

### Modify
- `Proton.Core/Interfaces/Repositories/ICacheRepository.cs` — add bar-specific query methods
- `Proton.Core/Interfaces/IChannelManager.cs` — remove market data channel
- `Proton.AppHost/Managers/ChannelManager.cs` — match interface
- `Proton.AppHost/Program.cs` — DI wiring for subscription manager + Redis
- `Proton.AppHost/appsettings.json` — Redis config + default symbols
- `Proton.Database.Redis/Proton.Database.Redis.csproj` — StackExchange.Redis package
- `Proton.AppHost/Utilities/GrpcMapper.cs` — remove unused mappings

### Evaluate for deletion
- `Proton.Core/Models/MarketData/MarketDataContext.cs`
- `Proton.Core/Models/MarketData/MarketDataRequest.cs`

---

## Verification

1. **Unit test `MarketDataSubscriptionManager`** — verify: (a) first subscriber starts upstream, (b) second subscriber reuses upstream, (c) last unsubscribe stops upstream, (d) bars are broadcast to all subscribers, (e) historical backfill occurs on first subscribe
2. **Unit test `RedisRepository`** — mock `IConnectionMultiplexer`, verify ZADD/ZREVRANGEBYSCORE calls and serialization
3. **Integration test** — start AppHost with `MockMarketDataProvider`, verify gRPC client receives bars via subscription path and data appears in Redis
4. **Manual test** — two gRPC clients subscribe to same symbol, verify only one upstream stream exists (check logs), both receive data. Disconnect one client, verify other still receives data. Disconnect both, verify upstream stops.
5. **Build** — `dotnet build Proton.Engine.slnx` passes with no errors
6. **Existing tests** — `dotnet test` passes for `Proton.Core.Tests` and `Proton.Backtesting.Tests`

---

## Decisions

- **Subscription manager lives in `Proton.MarketDataIngestion` project** — it's the natural home for market data orchestration
- **Redis sorted sets for bar storage** — allows efficient range queries by timestamp and automatic ordering
- **`TryWrite` for subscriber broadcast** — slow subscribers get dropped bars rather than causing backpressure on the upstream; this is appropriate for real-time market data
- **Historical backfill is automatic** — triggered on first subscription for a symbol, checking Parquet first then fetching from API
- **News ingestion is unchanged** — it's global (not per-symbol) and the channel pattern works fine for it
- **`MarketDataContext` / `MarketDataContextChannel` retired** — replaced entirely by the subscription manager
- **Scope includes**: subscription lifecycle, Redis implementation, historical backfill, gRPC client streaming, DI wiring
- **Scope excludes**: indicator calculation changes, backtesting module changes, trading service changes, news refactoring, proto schema changes (beyond optional additions)
