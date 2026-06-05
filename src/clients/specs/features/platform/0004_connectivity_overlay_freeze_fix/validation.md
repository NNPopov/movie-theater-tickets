# 0004 · connectivity_overlay_freeze_fix — Validation Checklist

## Manual testing

| # | Step | Expected result |
|---|---|---|
| M1 | Cold-start the app with the server reachable (first launch, never connected yet). | No "connection lost" dialog flashes; the app comes up normally. (F1, F7) |
| M2 | Cold-start the app while the first connection is still being established. | A non-permanent "reconnecting" spinner is shown over the app. (F2) |
| M3 | From the reconnecting state in M2, let the first connection succeed. | The reconnecting spinner disappears on its own without any tap. (F3) |
| M4 | While connected, kill the network briefly so the client goes `reconnecting` then reconnects on its own. | The app does not freeze; the spinner shows then clears; no error dialog gets stuck. (F3, F4) |
| M5 | Trigger a `reconnecting → connected` transition repeatedly (toggle the network several times). | Each transition completes; the app never freezes behind a stuck overlay and never crashes. (F4) |
| M6 | After the app has connected at least once, fully lose the connection. | A blocking "connection lost" dialog with a "reconnect" button is shown. (F5, F9) |
| M7 | From the M6 "connection lost" dialog, restore the connection on its own (do not press the button). | The "connection lost" dialog dismisses automatically. (F6) |
| M8 | From the M6 "connection lost" dialog, press the "reconnect" button. | A reconnect attempt is triggered (existing `connect()` behavior); on success the dialog clears. (F8) |
| M9 | While the reconnecting spinner or the "connection lost" dialog is shown, try to tap the app behind it (e.g. a menu item or seat). | The interaction is blocked; nothing behind the overlay reacts. (F9) |
| M10 | Switch the app language and re-trigger reconnecting / connection-lost. | The overlay header and reconnect button read in the selected language. (F10) |
| M11 | Disconnect before the app has ever connected (e.g. server down at launch). | No "connection lost" dialog appears; at most the reconnecting spinner during connect attempts. (F1, F7) |

## Code review

- [ ] `ConnectivitySafeAreaWidget` is a `StatefulWidget`; its `State` holds the overlay mode and a `wasConnected` flag, with no `late final` field reassignment. (N1)
- [ ] No `OverlayEntry`, `Overlay.of(context).insert`, or `Overlay.of(context).remove` remains in the file; the overlay is a layer in a `Stack`. (N2)
- [ ] The overlay-mode decision (incl. the "lost only after first connect" gate) is a pure top-level function taking `ConnectivityState` + `wasConnected`, with no `BuildContext`/widget args. (N3)
- [ ] `build()` selects the overlay widget for the current mode and contains no other logic. (N4)
- [ ] `setState` is used only to project `ConnectivityBloc` state into local view state; no second Cubit/Bloc was introduced. (N5)
- [ ] The widget reuses `OverlayDialog` and the three existing keys (`reconnecting_notification_text`, `connection_lost_notification_text`, `connection_lost_reconnect_btn`); no new strings added. (N6)
- [ ] The public constructor `ConnectivitySafeAreaWidget({super.key, required this.child})` is unchanged; `main.dart` is not modified. (N7)
- [ ] `git diff --name-only` shows only `connectivity_safe_area_widget.dart` (plus tests) changed; `connectivity_bloc.dart`, `signalr_event_hub.dart`, `injection_container.dart`, `main.dart`, and ARB files are untouched. (N8)
- [ ] `git diff pubspec.yaml` is empty — no new dependency added. (N9)
- [ ] Outside-in test `connectivity_overlay_freeze_fix_outside_in_test.dart` exists, drives the real `ConnectivityBloc` via a broadcast `StreamController<ConnectivityEvent>`, and covers all five contract cases. (N10)
- [ ] Unit tests for the resolver and widget tests (mocked `ConnectivityBloc`) for none/reconnecting/lost modes exist. (N11)
- [ ] No new `dart analyze` warning on the changed file (the pre-existing `override_on_non_overriding_member` on `ConnectivityBloc.connect()` is left for Audit Part 4). (N12)
- [ ] `dart run build_runner build --delete-conflicting-outputs` — no errors
- [ ] `dart run slang` — no errors
- [ ] `dart analyze` — no warnings
- [ ] All tests green
