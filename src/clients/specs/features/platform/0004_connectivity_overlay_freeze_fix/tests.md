# 0004 · connectivity_overlay_freeze_fix — Outside-in test spec

## Goal

Prove that the app-wide connectivity overlay is a pure function of connectivity state:
driving connectivity events through the real `ConnectivityBloc` shows/clears the correct
overlay (none / reconnecting spinner / connection-lost dialog) and, critically, a
`reconnecting → connected` transition completes **without throwing** (the original freeze
path).

> **Boundary note (why this test is widget-level, not Cubit-method-level).** Unlike a CRUD
> slice, the slice's public surface is *what the user sees on screen*, not a Cubit method.
> The defect is a `LateInitializationError` thrown during widget reaction to cubit state.
> So — exactly like the precedent migration test
> `0002_navigation_auto_route_shell_migration/navigation_shell_migration_outside_in_test.dart`
> (which gates at the router+widget level, not a Cubit method) — this test pumps the real
> widget under a real `ConnectivityBloc` and asserts externally observable outcomes (which
> overlay is visible, whether an exception was thrown). It never asserts `OverlayEntry`/
> `Stack` internals. The cubit is driven by feeding events into a fake `EventHub`, which is
> the one system boundary that is faked.

## Entry point

There is no Cubit method to call. The test drives the slice by **pushing connectivity
events into the fake `EventHub`'s `status` stream**, which the real `ConnectivityBloc`
listens to and turns into states, which `ConnectivitySafeAreaWidget` renders.

Example: `eventHub.emit(ReconnectingEvent()); await tester.pump();`
then `eventHub.emit(ConnectedEvent()); await tester.pump();`

## Wired real (production code in the test)

- `ConnectivitySafeAreaWidget` (the system under test — pumped inside a `MaterialApp` with
  the `AppLocalizations` delegates, mirroring its real placement as the app `builder`).
- `ConnectivityBloc` (the real cubit, provided via `BlocProvider`, seeded at
  `DisconnectedState`).
- `OverlayDialog` (real — its visible text/spinner/button are the assertion targets).
- The overlay-mode resolver (real — exercised indirectly through the widget).

## Mocked (system boundaries only)

- **`EventHub`**: a fake whose `status` getter is backed by a
  `StreamController<ConnectivityEvent>.broadcast()` the test controls. `subscribe()` and
  the other methods are inert no-ops. (Broadcast is mandatory: the widget holds a
  `BlocListener` and the cubit a stream listener — a single-subscription stream throws
  `Bad state: Stream has already been listened to`.)

## Test scenarios

### Scenario 1: `Reconnecting → Connected` completes without throwing, spinner cleared (core §2 regression)

**Setup:**
- Pump the widget with the real `ConnectivityBloc(fakeEventHub)` (seed `DisconnectedState`).

**Act:**
- `eventHub.emit(ReconnectingEvent()); await tester.pump();`
- `eventHub.emit(ConnectedEvent()); await tester.pump();`

**Expect:**
- `tester.takeException()` is `null` (previously `LateInitializationError`).
- The reconnecting spinner (`CircularProgressIndicator`) is **gone**.
- No connection-lost dialog is shown.

### Scenario 2: `Reconnecting` shows the reconnecting spinner

**Setup:**
- Pump the widget with the real `ConnectivityBloc(fakeEventHub)`.

**Act:**
- `eventHub.emit(ReconnectingEvent()); await tester.pump();`

**Expect:**
- A `CircularProgressIndicator` is visible.
- The reconnecting header text (`reconnecting_notification_text`) is visible.
- No connection-lost reconnect button is shown.

### Scenario 3: Cold-start `Disconnected` (never connected) shows no connection-lost dialog

**Setup:**
- Pump the widget with the real `ConnectivityBloc(fakeEventHub)` (seed `DisconnectedState`,
  no prior `ConnectedEvent`).

**Act:**
- `eventHub.emit(DisconnectedEvent()); await tester.pump();` (and/or rely on the seed).

**Expect:**
- The connection-lost header text (`connection_lost_notification_text`) is **absent**.
- The reconnect button (`connection_lost_reconnect_btn`) is **absent**.
- The wrapped child is visible (no blocking overlay).

### Scenario 4: `Connected` then `Disconnected` shows the connection-lost dialog with reconnect button

**Setup:**
- Pump the widget with the real `ConnectivityBloc(fakeEventHub)`.

**Act:**
- `eventHub.emit(ConnectedEvent()); await tester.pump();`
- `eventHub.emit(DisconnectedEvent()); await tester.pump();`

**Expect:**
- The connection-lost header text (`connection_lost_notification_text`) is visible.
- A reconnect button labelled `connection_lost_reconnect_btn` is visible.

### Scenario 5: `Connected` after a connection-lost dialog dismisses it automatically

**Setup:**
- Reach the Scenario-4 state (lost dialog shown after `Connected` then `Disconnected`).

**Act:**
- `eventHub.emit(ConnectedEvent()); await tester.pump();`

**Expect:**
- The connection-lost header text and reconnect button are **gone**.
- `tester.takeException()` is `null`.

## Out of scope for this test

- The pure resolver's full transition table (covered by the resolver unit test, written
  after green).
- Per-mode widget rendering with a mocked `ConnectivityBloc` (covered by the widget tests,
  written after green).
- Manual UX scenarios from `validation.md` that do not change the visible overlay
  (e.g. language switching, pointer-blocking) — verified manually / by widget tests.
- SignalR/EventHub internals and `ConnectivityBloc.connect()`'s real subscribe behavior
  (faked at the `EventHub` boundary).
