# PRD — Connectivity overlay freeze fix (Audit Part 2)

- **Slice:** `0004_connectivity_overlay_freeze_fix`
- **Feature:** platform
- **Type:** Tracked migration/defect-fix slice (legacy → target stack), single widget
- **Source:** `docs/audit/legacy-client-audit-2026-06-02.md` §2 + grilling session 2026-06-03
- **Status:** Planned

## Problem Statement

When the realtime connection drops and the app tries to reconnect, the screen can stay
covered by a modal "reconnecting…" / "connection lost" overlay that never goes away — the
app looks **frozen**. This is the hard lockup users actually report (distinct from the
"window-in-window" duplicate-screen symptom already fixed in slice 0002).

The connectivity overlay widget that sits above the whole app is a `StatelessWidget` that
illegally stores mutable overlay handles in `late final` fields and **reassigns** them on
every connectivity transition. A `late final` may be assigned only once; the second
assignment (and reading one before its first assignment) throws `LateInitializationError`.
This fires precisely on the main realtime path — `reconnecting → connected` and the first
`disconnected` transition — so a routine "connection lost → reconnecting" event can leave
the blocking overlay stuck on screen, and the app behind it becomes unusable.

On top of the crash, the overlay state is hand-synchronised against the connectivity state
by imperative insert/remove calls, and the synchronisation has gaps: the "connection lost"
dialog is not dismissed when the connection comes back on its own (only the manual
"reconnect" button removes it), and the reconnecting and lost overlays are not consistently
cleared relative to each other.

## Solution

Rebuild the connectivity overlay so that **what is shown is a pure function of the current
connectivity state**, eliminating the entire class of manual-synchronisation bugs rather
than only the `LateInitializationError`.

From the user's perspective:

- The app no longer freezes behind a stuck overlay when the connection drops and recovers.
- While the realtime connection is being (re)established, a non-permanent "reconnecting"
  spinner is shown; it disappears on its own once connected.
- If the connection is genuinely lost after having worked, a blocking "connection lost"
  dialog with a "reconnect" action is shown — and it goes away automatically the moment the
  connection is restored, not only when the button is pressed.
- On a cold start (before the app has ever connected), no "connection lost" dialog flashes;
  the user just sees the app come up (with the reconnecting spinner while the first
  connection is being made).

Internally, the widget becomes a `StatefulWidget` that derives a single overlay mode from
connectivity transitions and renders it declaratively in a `Stack`. No `OverlayEntry`, no
`Overlay.insert/remove`, no `late final` reassignment.

## User Stories

1. As a moviegoer, I want the app to keep working when my connection briefly drops and
   recovers, so that a momentary network blip does not freeze the whole app.
2. As a moviegoer, I want the "reconnecting" indicator to disappear by itself once the
   connection is back, so that I am not stuck staring at a spinner.
3. As a moviegoer, I want the "connection lost" dialog to close automatically when the
   connection is restored on its own, so that I do not have to tap "reconnect" to dismiss a
   dialog for a connection that already recovered.
4. As a moviegoer, I want to see a clear "connection lost" message with a way to retry only
   when the connection is actually lost after working, so that the message is meaningful.
5. As a moviegoer, I want the app to start cleanly without a "connection lost" dialog
   flashing on launch, so that the first impression is not a spurious error.
6. As a moviegoer, I want a reconnecting indicator while the app makes its first connection
   on launch, so that I understand the app is still coming up.
7. As a moviegoer selecting seats in realtime, I want the connectivity overlay to block
   interaction while the connection is down, so that I do not act on stale seat data.
8. As a moviegoer, I want the "reconnect" button to keep working as before, so that I can
   force a reconnect when I choose to.
9. As a moviegoer, I want the connectivity dialogs to read in my language, so that the
   message is understandable.
10. As a developer, I want the connectivity overlay to be driven purely by connectivity
    state, so that the overlay and the state can never desynchronise.
11. As a developer, I want the overlay widget to hold its UI state legally as a
    `StatefulWidget`, so that there is no `LateInitializationError` on connectivity changes.
12. As a developer, I want the overlay-mode decision (including the "lost only after first
    connect" rule) isolated as a small, pure, unit-testable seam, so that I can verify the
    state machine without pumping widgets.
13. As a developer, I want a red outside-in test that drives connectivity events through the
    real cubit into the widget, so that the slice has an acceptance gate that reproduces the
    original freeze path.
14. As a developer, I want widget tests for each observable overlay mode with a mocked cubit,
    so that none/reconnecting/lost rendering is locked.
15. As a maintainer, I want this fix tracked as its own slice scoped to a single widget, so
    that it is a deliberate step and not a side effect of unrelated work.
16. As a maintainer, I want the remaining audit findings (§3 SignalR/EventHub, §4 Bloc/Cubit)
    explicitly recorded as future slices, so that they are not forgotten while this slice is
    intentionally narrow.

## Implementation Decisions

**Approach — declarative, not imperative.** Replace the imperative `OverlayEntry` +
`Overlay.of(context).insert/remove` mechanism with declarative rendering: the overlay is a
layer in a `Stack` whose presence/content is a function of state. This removes the root
cause (manual synchronisation of imperative overlay handles against reactive state), not
just the `LateInitializationError`. Chosen over a minimal "keep `OverlayEntry`, move it to
`State` fields" patch because the minimal patch would leave the fragile manual sync (and its
known gaps) in place.

**Widget shape.** `ConnectivitySafeAreaWidget` becomes a `StatefulWidget`. A `BlocListener`
on `ConnectivityBloc` is the single place that maps connectivity transitions to a derived
overlay mode and updates it via `setState`; `build` is a pure function of that mode
(`Stack([child, <overlay for mode>])`). No logic in `build` beyond selecting the overlay
widget. This satisfies the project rule against logic in `build()` and the rule against
`setState` where a cubit exists by keeping `setState` confined to deriving local view state
from cubit state (the cubit is not replaced).

**Overlay mode + the "wasConnected" gate.** The derived state is a small enum:
`none` / `reconnecting` / `lost`. The widget keeps a `wasConnected` flag in its `State`.
The initial `DisconnectedState` (the cubit's seed state, before any connection) maps to
`none` (idle) — no "connection lost" dialog on cold start. `ReconnectingState` always maps
to `reconnecting` (including the first connection attempt). `DisconnectedState` maps to
`lost` **only after** the app has reached `ConnectedState` at least once. `ConnectedState`
maps to `none` and clears any overlay, including a previously shown "lost" dialog.

**Deep, testable seam.** The transition logic — `(previous mode/wasConnected, new
connectivity state) → (overlay mode, wasConnected)` — is isolated as a pure function/value
so it can be unit-tested in isolation without widget pumping. The widget is a thin shell
around it.

**Blocking semantics preserved.** The overlay layer absorbs pointer events (as the current
full-screen translucent `OverlayDialog` does), so the user cannot interact with the app
behind it while reconnecting/lost. This is intentional (e.g. no seat selection on stale
state).

**Reuse, no new surface.** Reuse the existing `OverlayDialog` widget and the existing three
`AppLocalizations` keys (`reconnecting_notification_text`,
`connection_lost_notification_text`, `connection_lost_reconnect_btn`). No new strings, no new
localization mechanism. The "reconnect" button keeps calling the existing
`ConnectivityBloc.connect()` as-is.

**Manual `get_it` / no new dependencies.** No new packages. No `injectable`, no `slang`.
`ConnectivityBloc` and `EventHub` wiring is unchanged.

**Behavioral contract:**

- Initial `Disconnected` (never connected) → no dialog (idle).
- `Reconnecting` → reconnecting spinner (non-permanent), on first connect too.
- `Connected` after `Reconnecting` → no overlay, **no exception thrown** (the core §2 fix).
- `Connected` after `Disconnected`-while-connected → "lost" dialog cleared automatically.
- `Disconnected` after a prior `Connected` → blocking "connection lost" dialog + reconnect
  button.

## Testing Decisions

**What a good test is here.** Tests assert externally observable behavior — which overlay
is visible (none / reconnecting spinner / lost dialog), that a transition completes without
throwing, and that the lost dialog clears on recovery — never the internal
`OverlayEntry`/`Stack` mechanics or `auto_route`/SignalR internals.

**Outside-in acceptance gate.** One file (e.g.
`connectivity_overlay_freeze_fix_outside_in_test.dart`) driven through the **real**
`ConnectivityBloc` fed by a **fake `EventHub`** (a `StreamController<ConnectivityEvent>` the
test controls). This reproduces the exact original crash path (event → cubit state →
overlay). Five red cases that form the contract:

1. `Reconnecting → Connected` completes **without a thrown exception**, and the spinner is
   gone. (Core §2 regression — previously `LateInitializationError`.)
2. `Reconnecting` shows the reconnecting spinner.
3. Cold-start `Disconnected` (no prior `Connected`) shows **no** "connection lost" dialog.
4. `Connected` then `Disconnected` shows the "connection lost" dialog with a reconnect
   button.
5. After "lost", a `Connected` event dismisses the "connection lost" dialog.

**Default layer tests.** Per the project default coverage: widget tests with a **mocked
`ConnectivityBloc`** (mocktail), one case per observable overlay mode
(none / reconnecting / lost). Unit tests for the extracted pure overlay-mode resolver
(transition table incl. the "lost only after first connect" gate). There is no new network
adapter or use-case and the cubit is not modified in this slice, so the adapter/use-case
default layers do not apply; the cubit layer is covered indirectly via the outside-in test.

**Prior art.** Slice 0002's
`navigation_shell_migration_outside_in_test.dart` (one file, multiple red cases gating the
changed behaviors of a migration slice) and slice 0001's migration outside-in test. Module G
smoke tests remain the broader safety net. Use `bloc_test`/`mocktail` per the project test
conventions.

## Out of Scope

These remain deliberate non-actions / future tracked slices (also recorded in the audit
doc's "Follow-up tracking" section):

- **§3 SignalR/EventHub robustness** — broadcast `StreamController`, init `_hubConnection`
  before first use, catch-all `catch (e, st)` + logging in handlers, remove the double
  `jsonDecode(jsonEncode(...))`. → future slice **"Audit Part 3"**.
- **§4 Bloc/Cubit cleanup** — the false `@override connect()` on `ConnectivityBloc` (a
  `Cubit`), `print()` statements in `connectivity_bloc.dart`, network side effects in
  constructors. → future slice **"Audit Part 4"**. Because this slice does not touch
  `connectivity_bloc.dart`, the pre-existing `override_on_non_overriding_member` warning
  stays until Part 4; the analyze gate for slice 0004 is scoped to its changed files.
- **Eager `subscribe()` at DI registration time** (`injection_container.dart`) — related to
  §3; not touched here.
- **Renaming the misspelled `widgens/` folder** or moving the file out of `lib/src/hub/` —
  legacy location preserved (renaming breaks imports, scope creep).
- **Migrating the `hub` feature to the target architecture** (ports/adapters), swapping
  `AppLocalizations → slang`, renaming `*Bloc`/`*Cubit`.
- **Inline `TextStyle` styling** in the overlay — left as-is (visual parity).
- **Changing the connectivity state model** (`ConnectivityBloc` states/events) — the widget
  adapts to the existing states; the cubit is not modified.

## Further Notes

- This is the freeze the audit flagged as **Priority #1** ("most likely freeze"); slice 0002
  (navigation) intentionally deferred it. Navigation fixed the "window-in-window" /
  duplicate-screen and masked-error symptoms; this slice targets the hard lockup.
- The overlay sits in `main.dart` via `MaterialApp.router`'s `builder` wrapping the whole
  app `child`. A `Stack` inside that wrapper covers the same visual area the root `Overlay`
  did, so blocking/visual parity holds without using the `Overlay` API.
- The audit-driven client backlog is tracked in three places so §3/§4 are not lost: the
  audit doc (source of truth, updated), this PRD's Out of Scope, and planned roadmap rows to
  be added when those slices are taken up.
- No external issue tracker is configured for the client; per project rules this PRD is saved
  locally in the slice folder rather than published to a remote tracker.
