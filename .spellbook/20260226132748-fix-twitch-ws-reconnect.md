# Spell: Fix Twitch WebSocket Spurious Disconnect/Reconnect Loop

**Task ID:** 20260226132748  
**Status:** Plan Complete — Ready for Implementation  
**Created:** 2026-02-26

## Problem

The Caramel Twitch service keeps disconnecting and reconnecting its WebSocket connection every ~10-12 seconds, even though while connected it still works correctly. The cycle matches TwitchLib's keepalive watchdog timeout (Twitch default: 10s + 20% grace = ~12s).

## Root Causes

| # | Cause | Severity |
|---|-------|----------|
| 1 | **`ReconnectAsync()` used as `ConnectAsync()` fallback** — wrong API. `ReconnectAsync()` disconnects the existing socket and starts fresh, putting the client into a bad state when called on a never-connected socket. | 🔴 Critical |
| 2 | **`_disconnectTcs` race condition** — TCS is reset *before* `ConnectAsync()`, so a stale disconnect event can trigger the new TCS immediately | 🔴 Critical |
| 3 | **`WebsocketReconnected` event not wired** — server-initiated reconnects are invisible | 🟡 Medium |
| 4 | **`appsettings.json` uses `"Apollo"` logging namespace** — all Caramel debug logs are invisible in Docker | 🟡 Medium |
| 5 | **Startup token assumed valid for 1 hour** — stale tokens cause silent 401s → 4003 close | 🟡 Medium |
| 6 | **`HttpClient` disposed via `using`** — `IHttpClientFactory` manages lifetime; manual dispose is incorrect | 🟡 Medium |
| 7 | **`CancellationToken.None` in registrar calls** — shutdown can hang waiting for HTTP calls | 🟢 Low |

## TwitchLib v0.8.0 Key Findings

- `ConnectAsync()`: Connects + starts `ConnectionCheckAsync` background loop (keepalive watchdog)
- `ReconnectAsync()` has two paths:
  - **Path A** (`_reconnectRequested=true`): INTERNAL ONLY — opens new socket to Twitch-provided reconnect URL, swaps atomically, fires `WebsocketReconnected`. Does NOT fire `WebsocketDisconnected`.
  - **Path B** (public `ReconnectAsync()`): Disconnects old, creates new socket, calls `ConnectAsync()`, fires `WebsocketReconnected`.
- Keepalive: library handles `session_keepalive` internally by updating `_lastReceived`. `ConnectionCheckAsync` fires `WebsocketDisconnected` if no message for `timeout * 1.2`.
- Server-initiated reconnect: Library calls `ReconnectAsync(reconnect_url)` internally. `WebsocketDisconnected` is NOT fired. `WebsocketConnected` fires with `IsRequestedReconnect=true`.
- Known Issue #48 (open): Intermittent "Failed Ping Pong" 4002 errors — low-level .NET WebSocket issue, not TwitchLib.

## Acceptance Tests (25 methods)

### Core Lifecycle (11 tests)

1. `ExecuteAsync_WhenTokenAvailableAndSetupConfigured_ConnectsEventSub`
2. `ExecuteAsync_WhenSetupNotConfigured_PollsUntilConfigured`
3. `ExecuteAsync_WhenNoRefreshToken_WaitsAndRetries`
4. `ExecuteAsync_WhenConnectAsyncReturnsFalse_DoesNotCallReconnectAsync`
5. `ExecuteAsync_WhenConnectAsyncReturnsFalse_RetriesAfterDelay`
6. `ExecuteAsync_WhenWebsocketDisconnects_ReconnectsAfterDelay`
7. `ExecuteAsync_WhenStoppingTokenCancelled_ExitsCleanly`
8. `ExecuteAsync_WhenGenericExceptionThrown_RetriesAfterTenSeconds`
9. `StopAsync_WhileConnected_DisconnectsGracefully`
10. `WireEventHandlers_CalledMultipleTimes_WiresOnlyOnce`
11. `ExecuteAsync_WhenDisconnectAndStopRace_HandlesIdempotently`

### Event Handlers (8 tests)

12. `OnWebsocketConnected_WhenNotReconnect_RegistersAllSubscriptions`
13. `OnWebsocketConnected_WhenIsReconnect_SkipsRegistration`
14. `OnWebsocketConnected_WhenSetupNull_LogsAndReturns`
15. `OnWebsocketConnected_WhenRegistrarThrows_CatchesAndLogs`
16. `OnWebsocketConnected_PassesCancellationTokenToRegistrars`
17. `OnWebsocketReconnected_LogsReconnectionEvent`
18. `OnWebsocketDisconnected_SignalsDisconnectTcs`
19. `OnWebsocketConnected_DuplicateNonReconnectEvents_DoNotCrash`

### Edge Cases (6 tests)

20. `ExecuteAsync_StaleDisconnectOnFreshTcs_DoesNotDoubleConnect`
21. `OnWebsocketConnected_StopAsyncDuringRegistration_DoesNotHang`
22. `OnChannelChatMessage_WhenMediatorThrows_DoesNotCrashService`
23. `OnUserWhisperMessage_WhenMediatorThrows_DoesNotCrashService`
24. `ExecuteAsync_ConsecutiveFailures_UsesFixedFiveSecondDelay`
25. `OnWebsocketConnected_HttpClientNotManuallyDisposed`

## Units of Work

### UoW-1: Extract `IEventSubWebsocketClientWrapper` Interface + Adapter
**Dependencies:** None  
**Phase:** 1 (parallel with UoW-2)

- Create `IEventSubWebsocketClientWrapper` interface exposing: `ConnectAsync()`, `DisconnectAsync()`, `SessionId`, and events (`WebsocketConnected`, `WebsocketDisconnected`, `WebsocketReconnected`, `ChannelChatMessage`, `UserWhisperMessage`, `ChannelPointsCustomRewardRedemptionAdd`)
- Create `EventSubWebsocketClientWrapper` that delegates to TwitchLib's concrete `EventSubWebsocketClient`
- Update DI registration in `ServiceCollectionExtension.cs`
- **No behavior change** — pure refactor for testability

### UoW-2: Extract `ITwitchTokenManager` Interface
**Dependencies:** None  
**Phase:** 1 (parallel with UoW-1)

- Create `ITwitchTokenManager` interface with: `GetValidAccessTokenAsync()`, `SetTokens()`, `GetCurrentAccessToken()`, `CanRefresh()`
- Update `TwitchTokenManager` to implement `ITwitchTokenManager`
- Update DI registration to register as `ITwitchTokenManager`
- Update all consumers (`EventSubLifecycleService`, `AdsController`, `AuthController`, `TwitchUserResolver`) to depend on `ITwitchTokenManager`
- **No behavior change** — pure refactor for testability

### UoW-3: Restructure `EventSubLifecycleService`
**Dependencies:** UoW-1, UoW-2  
**Phase:** 2

- Depend on `IEventSubWebsocketClientWrapper` and `ITwitchTokenManager` instead of concrete types
- **Remove** the `ReconnectAsync()` fallback in `TryConnectEventSubAsync`
- **Wire** `WebsocketReconnected` event handler (logging only)
- **Fix** `_disconnectTcs` race: create TCS only *after* successful `ConnectAsync`
- **Remove** `using var httpClient` — let `IHttpClientFactory` manage lifetime
- **Pass** `stoppingToken` to registrar calls instead of `CancellationToken.None`
- Follow TwitchLib's official pattern: simple `StartAsync` → `ConnectAsync`, reconnect logic in `OnWebsocketDisconnected`

### UoW-4: Fix `TwitchTokenManager` Startup Token Assumption
**Dependencies:** UoW-2 (interface must exist)  
**Phase:** 3 (parallel with UoW-5, UoW-6)

- Change `DateTime.UtcNow.AddHours(1)` to `DateTime.MinValue` for startup tokens
- This forces a token validation/refresh on first use, preventing stale-token 401s
- Update existing `TwitchTokenManagerTests` to cover this behavior

### UoW-5: Fix `appsettings.json` Logging Namespace
**Dependencies:** None (but logically Phase 3)  
**Phase:** 3 (parallel with UoW-4, UoW-6)

- Change `"Apollo": "Debug"` → `"Caramel": "Debug"` in `appsettings.json`
- Quick config-only change

### UoW-6: `EventSubLifecycleService` Acceptance Tests
**Dependencies:** UoW-3 (restructured service must exist)  
**Phase:** 3 (parallel with UoW-4, UoW-5)

- Create `tests/Caramel.Twitch.Tests/Services/EventSubLifecycleServiceTests.cs`
- Implement all 25 acceptance test methods listed above
- Mock `IEventSubWebsocketClientWrapper`, `ITwitchTokenManager`, `ITwitchSetupState`, `IMediator`, `IHttpClientFactory`, `IEventSubSubscriptionRegistrar`

## Execution Order

```
Phase 1 (parallel):   UoW-1  |  UoW-2
Phase 2 (sequential): UoW-3 (depends on 1+2)
Phase 3 (parallel):   UoW-4  |  UoW-5  |  UoW-6 (depends on 3)
```
