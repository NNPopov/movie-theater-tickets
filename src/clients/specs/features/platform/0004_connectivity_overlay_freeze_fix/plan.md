# Feature Spec / Implementation Plan — `0004_connectivity_overlay_freeze_fix`

- **Slice:** `0004_connectivity_overlay_freeze_fix`
- **Feature:** platform
- **Type:** Tracked migration/defect-fix slice (legacy → target intent), **single widget**
- **Sources:** `prd.md` (this folder) + the legacy connectivity code under `lib/src/hub/`
- **Status:** Planned

> This is a **defect-fix** slice, not a CRUD slice. There is **no backend endpoint** and
> **no new use-case, adapter, DTO, or Cubit**. The "contract" the implementation must
> honor is an **overlay-mode contract**: a pure function from connectivity transitions to
> which overlay is on screen. Block 3 below replaces the usual "API" block with that
> contract.

---

## 1. Title

Rebuild the app-wide connectivity overlay (`ConnectivitySafeAreaWidget`) so that **what is
shown is a pure function of the current connectivity state**. Today the widget is a
`StatelessWidget` that stores `OverlayEntry` handles in `late final` fields and
**reassigns** them on every connectivity transition — illegal for `late final`, so the
main realtime path (`reconnecting → connected`, first `disconnected`) throws
`LateInitializationError` and can leave a blocking overlay stuck on screen: the app looks
**frozen**. Outcome for the user: a momentary network blip no longer freezes the app; the
"reconnecting" spinner disappears on its own once connected; the "connection lost" dialog
clears automatically when the connection recovers (not only on the manual button); and no
"connection lost" dialog flashes on cold start. The "reconnect" button keeps working as
before. This is the Priority #1 freeze the audit flagged, deliberately deferred by slice
0002.

---

## 2. Context

### READ

- `@CLAUDE.md` — fully (locked stack, migration rules, hard rules: no logic in `build()`,
  `setState`-vs-Cubit rule, adapter catch-all, no new deps without approval).
- `@specs/features/platform/0004_connectivity_overlay_freeze_fix/prd.md` — the PRD this
  plan implements (behavioral contract, the `wasConnected` gate, scope fences).
- **The single file being rewritten:**
  - `@lib/src/hub/presentation/widgens/connectivity_safe_area_widget.dart` — the buggy
    `StatelessWidget` with `late final OverlayEntry?` reassignment. (Folder name
    `widgens/` is a known misspelling — **do not rename it**, scope creep / import churn.)
- **Files read for signatures only (NOT modified):**
  - `@lib/src/hub/presentation/cubit/connectivity_bloc.dart` — the `ConnectivityBloc`
    (a `Cubit`), its states `DisconnectedState` / `ReconnectingState` / `ConnectedState`
    (seed = `DisconnectedState()`), and `connect()` (calls `_eventHub.subscribe()`). The
    cubit is **not modified** in this slice. Its `print()` calls and the false
    `@override connect()` are Audit Part 4 — leave them.
  - `@lib/src/hub/domain/event_hub.dart` — `EventHub.status` is a
    `Stream<ConnectivityEvent>`; the events are `DisconnectedEvent` / `ReconnectingEvent`
    / `ConnectedEvent` (defined in `connectivity_bloc.dart`). The outside-in test feeds a
    fake `EventHub` whose `status` is a `StreamController<ConnectivityEvent>.broadcast()`.
  - `@lib/core/common/widgets/overlay_dialog.dart` — `OverlayDialog({required header,
    required body, ...})`. Reuse as-is; its translucent full-screen `Container` is what
    provides the visual + pointer-blocking layer.
  - `@lib/main.dart` — the overlay is wired via `MaterialApp.router`'s
    `builder: (context, child) => ConnectivitySafeAreaWidget(child: child!)`, under a
    `BlocProvider<ConnectivityBloc>`. The wiring is **unchanged**; the widget keeps the
    same public constructor `ConnectivitySafeAreaWidget({super.key, required this.child})`.
  - `@lib/l10n/app_en.arb` / `app_es.arb` / `app_ru.arb` — the three **existing** keys
    `reconnecting_notification_text`, `connection_lost_notification_text`,
    `connection_lost_reconnect_btn`. **Reuse; add no new keys.**
- **Prior art / test idiom:**
  - `@test/features/platform/0002_navigation_auto_route_shell_migration/navigation_shell_migration_outside_in_test.dart`
    — migration outside-in shape (one file, several cases, real plumbing + a single
    mocked boundary; inert cubit stubs; `AppLocalizations` delegates wired in the test).
  - `@agent_docs/testing.md` — mocktail conventions, `bloc_test`, the broadcast-stream
    gotcha (a widget with both `BlocBuilder` and `BlocListener` needs
    `StreamController.broadcast()`), and `MockCubit`/`whenListen` for widget tests.

### DO NOT READ

- Any other slice's `data/`, `domain/`, or `application/`. This slice touches one widget.
- The SignalR adapter `@lib/src/hub/data/signalr_event_hub.dart` — its robustness issues
  are **Audit Part 3**, explicitly out of scope. Reading it tempts scope creep.
- `@lib/injection_container.dart` — DI wiring is unchanged (the eager `subscribe()` is
  Part 3). Do not touch.
- Other features' specs.

---

## 3. Overlay-mode contract (replaces the "API" block)

There is **no HTTP**. The implementation must realize this exact mapping. The derived view
state is a small mode enum — `none` / `reconnecting` / `lost` — plus a `wasConnected`
latch held in widget `State`.

### State machine

`wasConnected` starts `false`. It flips to `true` the first time `ConnectedState` is seen
and never flips back. The overlay mode is derived from the **current** connectivity state
and `wasConnected`:

| Connectivity state | `wasConnected` | Overlay mode | Notes |
|---|---|---|---|
| `DisconnectedState` (seed / cold start) | `false` | `none` | **No "connection lost" dialog on launch.** |
| `ReconnectingState` | any | `reconnecting` | Non-permanent spinner; same for the first connection attempt. |
| `ConnectedState` | (sets `true`) | `none` | Clears any overlay, including a shown "lost" dialog. **No exception** — the core §2 fix. |
| `DisconnectedState` | `true` | `lost` | Blocking "connection lost" dialog + reconnect button. |

### Overlay-mode rendering

- `none` → render `child` only (no overlay layer).
- `reconnecting` → `child` + an `OverlayDialog` whose header is
  `reconnecting_notification_text` and body is a `CircularProgressIndicator`
  (50×50 `SizedBox`, parity with today).
- `lost` → `child` + an `OverlayDialog` whose header is
  `connection_lost_notification_text` and body is a `TextButton`
  (`connection_lost_reconnect_btn`) that calls `ConnectivityBloc.connect()`. The button
  no longer needs to imperatively remove anything — recovery (a later `ConnectedState`)
  clears the dialog declaratively.

### Behavioral guarantees (the acceptance contract)

1. `Reconnecting → Connected` completes **without a thrown exception**, and the spinner is
   gone. (Core §2 regression — previously `LateInitializationError`.)
2. `Reconnecting` shows the reconnecting spinner.
3. Cold-start `Disconnected` (no prior `Connected`) shows **no** "connection lost" dialog.
4. `Connected` then `Disconnected` shows the "connection lost" dialog with a reconnect
   button.
5. After "lost", a `Connected` event **dismisses** the "connection lost" dialog.

### Blocking semantics (parity)

The overlay layer absorbs pointer events for `reconnecting` and `lost` (the translucent
full-screen `OverlayDialog` does this today). Interaction with the app behind it stays
blocked while reconnecting/lost — intentional (no seat selection on stale state).

---

## 4. Target structure

A single file is rewritten in place; the test tree gains the slice folder. **No new
production files, no new strings, no new dependencies.**

```
lib/src/hub/presentation/widgens/
└── connectivity_safe_area_widget.dart   # REWRITTEN: StatelessWidget → StatefulWidget.
                                          #   - State holds: _ConnectivityOverlayMode _mode,
                                          #     bool _wasConnected.
                                          #   - BlocListener<ConnectivityBloc,...> maps each
                                          #     transition to (mode, wasConnected) via the
                                          #     pure resolver, then setState.
                                          #   - build() returns Stack([child, overlayForMode])
                                          #     — pure function of _mode, NO logic.
                                          #   - No OverlayEntry, no Overlay.insert/remove,
                                          #     no late final reassignment.
```

Inside the same file, two private seams (kept in one file — single-widget slice, no new
public surface):

```
enum _ConnectivityOverlayMode { none, reconnecting, lost }

// Pure, unit-testable resolver — no Flutter imports needed for its logic.
// (prevMode/wasConnected, ConnectivityState) -> (mode, wasConnected)
_OverlayResolution resolveOverlayMode(
  ConnectivityState state, { required bool wasConnected });
```

> The resolver is the "deep, testable seam" from the PRD. Make `resolveOverlayMode` and
> `_ConnectivityOverlayMode` **library-private but importable from the test** (top-level
> declarations in the widget file, so a `test` import of the widget file can reach them).
> Keep the result a tiny value (`(_ConnectivityOverlayMode mode, bool wasConnected)` record
> or a small `_OverlayResolution` class with `==`). It depends only on `ConnectivityState`
> subtypes — no `BuildContext`, no widgets — so it is testable without pumping.

```
test/features/platform/0004_connectivity_overlay_freeze_fix/
└── connectivity_overlay_freeze_fix_outside_in_test.dart   # RED acceptance gate (this slice).
                                                            # Later (post-green): the default
                                                            # layer tests in §6.b/§6.c.
```

---

## 5. What to do — step by step

> One production file. No DI change, no `main.dart` change, no localization change, no new
> dependency. If you find yourself editing anything outside
> `connectivity_safe_area_widget.dart` (production), **stop and ask** — that is a signal of
> scope creep into Part 3/Part 4.

### Step 1 — Define the overlay mode + pure resolver (top of the widget file)

- `enum _ConnectivityOverlayMode { none, reconnecting, lost }`.
- A pure top-level function `resolveOverlayMode(ConnectivityState state, {required bool
  wasConnected})` returning the new `(mode, wasConnected)` per the table in §3:
  - `ReconnectingState` → `(reconnecting, wasConnected)`.
  - `ConnectedState` → `(none, true)`.
  - `DisconnectedState` → `wasConnected ? (lost, true) : (none, false)`.
- No Flutter types in this function's signature beyond the `ConnectivityState` it switches
  on. This is the unit-test seam.

### Step 2 — Convert the widget to `StatefulWidget`

- Keep the **same public constructor**: `ConnectivitySafeAreaWidget({super.key, required
  this.child})` with `final Widget child;` on the widget, so `main.dart` is untouched.
- `State` fields: `_ConnectivityOverlayMode _mode = _ConnectivityOverlayMode.none;` and
  `bool _wasConnected = false;`. Seed mode `none` matches the cubit's `DisconnectedState`
  seed (cold start, guarantee #3).

### Step 3 — `BlocListener` derives the mode (the only place with logic)

- `build` returns:
  ```dart
  BlocListener<ConnectivityBloc, ConnectivityState>(
    listener: (context, state) {
      final next = resolveOverlayMode(state, wasConnected: _wasConnected);
      if (next.mode != _mode || next.wasConnected != _wasConnected) {
        setState(() {
          _mode = next.mode;
          _wasConnected = next.wasConnected;
        });
      }
    },
    child: Stack(children: [widget.child, ..._overlayLayer(context)]),
  )
  ```
- `_overlayLayer(context)` is a pure switch on `_mode` returning `[]`, the reconnecting
  `OverlayDialog`, or the lost `OverlayDialog`. **No logic in `build` beyond selecting the
  overlay widget** — satisfies the hard rule.
- **CRITICAL — `setState` legality.** The hard rule "❌ `setState` in a widget that has a
  Cubit" is satisfied here: `setState` is confined to deriving *local view state* from the
  cubit's state (the cubit is the single source of truth; we are not duplicating its job,
  we are projecting it). This is the same exception the PRD calls out. Do **not** reach for
  a second Cubit — that is over-engineering a single overlay projection.

### Step 4 — Render the overlay layers (reuse `OverlayDialog`)

- `reconnecting`: `OverlayDialog(header: Text(AppLocalizations.of(context)!.reconnecting_notification_text, style: <parity TextStyle>), body: const SizedBox(width: 50, height: 50, child: CircularProgressIndicator()))`.
- `lost`: `OverlayDialog(header: Text(...connection_lost_notification_text...), body: SizedBox(width: 120, height: 50, child: TextButton(onPressed: () => context.read<ConnectivityBloc>().connect(), child: Text(...connection_lost_reconnect_btn...))))`.
- Keep the existing inline `TextStyle` (visual parity — restyling is out of scope).
- The `OverlayDialog`'s translucent full-screen `Container` preserves blocking + visual
  parity without the `Overlay` API.

### Step 5 — Delete the imperative machinery

- Remove both `late final OverlayEntry?` fields, every `OverlayEntry(...)`,
  `Overlay.of(context).insert(...)`, `.remove()`, and the manual `connectivityBloc` read
  for imperative removal. None of it survives — the `Stack` + mode is the whole mechanism.

### Step 6 — Verify

- `dart format .` — no diff. `dart analyze` — no **new** warnings on the changed file.
  (The pre-existing `override_on_non_overriding_member` on `ConnectivityBloc.connect()` is
  Audit Part 4 and stays; the analyze gate for this slice is scoped to its changed file.)
- **No** `build_runner` needed (no freezed/injectable/retrofit/slang touched).
- **No** slang regen (no `*.json` under `lib/core/i18n/` touched — this app still uses the
  legacy `AppLocalizations`/ARB, untouched here).

---

## 6. Tests

> Per CLAUDE.md default coverage, **adapted** to this slice: there is **no adapter or
> use-case introduced**, and the **cubit is not modified** (covered indirectly via the
> outside-in test), so those default layers do not apply. The observable surfaces are the
> overlay rendering (widget) and the pure resolver (unit).

`test/features/platform/0004_connectivity_overlay_freeze_fix/`:

### a) `connectivity_overlay_freeze_fix_outside_in_test.dart` — the RED acceptance gate

One file driving connectivity events through the **real `ConnectivityBloc`** fed by a
**fake `EventHub`** (a `StreamController<ConnectivityEvent>.broadcast()` the test pushes
into). This reproduces the exact original crash path: event → cubit state → overlay. The
broadcast controller is mandatory — the widget holds a `BlocListener` (and the app a
`BlocProvider`), so a single-subscription stream throws. Five cases = the §3 contract:

1. **`Reconnecting → Connected` throws nothing, spinner gone.** Push `ReconnectingEvent`
   then `ConnectedEvent`; pump; assert `tester.takeException()` is null and the spinner is
   absent. (Core §2 regression.)
2. **`Reconnecting` shows the spinner.** Push `ReconnectingEvent`; assert a
   `CircularProgressIndicator` (or the reconnecting `OverlayDialog`) is visible.
3. **Cold-start `Disconnected` shows no lost dialog.** With no prior `ConnectedEvent`,
   push `DisconnectedEvent` (or rely on the seed); assert the
   `connection_lost_notification_text` / reconnect button is **absent**.
4. **`Connected` then `Disconnected` shows the lost dialog + reconnect button.** Push
   `ConnectedEvent`, then `DisconnectedEvent`; assert the lost dialog and the reconnect
   button are visible.
5. **`Connected` after lost dismisses the lost dialog.** From case-4 state push
   `ConnectedEvent`; assert the lost dialog is gone.

**Expected RED reason at write time:** the widget is still the buggy `StatelessWidget`;
case 1 throws `LateInitializationError` and cases 3/5 fail the desync assertions. It turns
GREEN only once the rewrite per §5 lands.

### b) Unit test for the pure resolver (default — replaces the use-case layer)

`presentation/connectivity_overlay_mode_resolver_test.dart` (or co-located) — a plain
`test(...)` table over `resolveOverlayMode`:

- seed `Disconnected`, `wasConnected:false` → `(none, false)`.
- `Reconnecting`, any `wasConnected` → `(reconnecting, wasConnected)`.
- `Connected`, `wasConnected:false` → `(none, true)`.
- `Disconnected`, `wasConnected:true` → `(lost, true)`.
- `Disconnected`, `wasConnected:false` → `(none, false)` (the cold-start gate).

### c) Widget tests with a mocked `ConnectivityBloc` (default — one per observable mode)

`presentation/connectivity_safe_area_widget_test.dart` — `MockCubit<ConnectivityState>` +
`whenListen` (per `agent_docs/testing.md`), `AppLocalizations` delegates wired:

- mode `none` (seed `Disconnected`, never connected) → no overlay; `child` visible.
- mode `reconnecting` → reconnecting spinner visible.
- mode `lost` (after `Connected` then `Disconnected`) → lost dialog + reconnect button
  visible.

### d) Safety net (not new — must stay green)

- The Module G smoke tests remain the broader safety net and must stay green.

---

## 7. Report (what the implementing agent must hand back)

- Files created / modified (expect: `connectivity_safe_area_widget.dart` **rewritten**;
  the outside-in test created; then resolver + widget unit tests created post-green).
- Confirmation that **no** other production file was touched — not `connectivity_bloc.dart`,
  not `signalr_event_hub.dart`, not `injection_container.dart`, not `main.dart`, not the
  ARBs, and **no** new dependency added.
- Confirmation the public constructor is unchanged (so `main.dart` compiles untouched).
- The outside-in test is **RED for the right reason** before implementation (case 1 =
  `LateInitializationError`), and **GREEN** after, with the Module G smoke tests green.
- `dart format` clean; `dart analyze` clean on the changed file (pre-existing
  `ConnectivityBloc` warning explicitly excluded — Part 4).

---

## 8. What NOT to do (scope fences)

- ❌ Do **not** keep `OverlayEntry` / `Overlay.of(context).insert/remove` "to minimize the
  diff". The declarative `Stack` + mode is the chosen approach — it removes the manual-sync
  bug class, not just the `LateInitializationError`.
- ❌ Do **not** touch `connectivity_bloc.dart` — not the `print()`s, not the false
  `@override connect()`, not its state/event model. That is **Audit Part 4**.
- ❌ Do **not** touch `signalr_event_hub.dart` (broadcast controller, init order,
  catch-all logging, double `jsonDecode(jsonEncode(...))`). That is **Audit Part 3**.
- ❌ Do **not** touch `injection_container.dart` (eager `subscribe()` at DI time — Part 3)
  or `main.dart` (wiring is unchanged).
- ❌ Do **not** add new localization keys or migrate `AppLocalizations → slang`. Reuse the
  three existing keys.
- ❌ Do **not** add any dependency to `pubspec.yaml`.
- ❌ Do **not** rename the misspelled `widgens/` folder or move the file out of
  `lib/src/hub/` — renaming breaks imports (scope creep).
- ❌ Do **not** restyle the overlay (keep the inline `TextStyle`) or change the dialog
  sizes — visual parity.
- ❌ Do **not** introduce a second Cubit/Bloc for the overlay mode. The mode is a local
  projection of `ConnectivityBloc`'s state; a `setState`-driven `State` field is correct
  here and the rewrite stays a single widget.
