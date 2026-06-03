# 0004 · connectivity_overlay_freeze_fix — Requirements

## Functional Requirements

| ID | Requirement |
|---|---|
| F1 | The connectivity overlay shows nothing (no dialog, no spinner) on cold start before the app has ever connected. |
| F2 | While the realtime connection is being (re)established, the system shows a non-permanent "reconnecting" spinner. |
| F3 | The reconnecting spinner disappears on its own once the connection becomes connected, with no action from the user. |
| F4 | A `Reconnecting → Connected` transition completes without throwing any exception. |
| F5 | After the app has connected at least once, a subsequent loss of connection shows a blocking "connection lost" dialog with a "reconnect" action. |
| F6 | The "connection lost" dialog is dismissed automatically the moment the connection is restored, not only when the reconnect button is pressed. |
| F7 | A disconnection that occurs before the app has ever connected does not show the "connection lost" dialog. |
| F8 | The "reconnect" button triggers the existing `ConnectivityBloc.connect()` behavior. |
| F9 | The reconnecting and "connection lost" overlays block interaction with the app behind them while shown. |
| F10 | Overlay text is rendered through the existing localization keys so it reads in the user's selected language. |
| F11 | Which overlay is shown is a pure function of the current connectivity state and whether the app has previously connected — the overlay and the connectivity state can never desynchronize. |

## Non-functional Requirements

| ID | Requirement |
|---|---|
| N1 | `ConnectivitySafeAreaWidget` is a `StatefulWidget` and holds its overlay mode and `wasConnected` latch in `State`, with no `late final` field reassignment (no `LateInitializationError`). |
| N2 | The overlay uses declarative rendering (a layer in a `Stack`) and contains no `OverlayEntry`, `Overlay.of(context).insert`, or `Overlay.of(context).remove` calls. |
| N3 | The overlay-mode decision, including the "lost only after first connect" gate, is isolated as a pure, unit-testable function with no `BuildContext` or widget dependency. |
| N4 | The widget's `build()` contains no logic beyond selecting the overlay widget for the current mode. |
| N5 | `setState` is used only to project `ConnectivityBloc`'s state into local view state; no second Cubit/Bloc is introduced for the overlay mode. |
| N6 | The widget reuses the existing `OverlayDialog` widget and the three existing `AppLocalizations` keys (`reconnecting_notification_text`, `connection_lost_notification_text`, `connection_lost_reconnect_btn`); no new strings are added. |
| N7 | The widget's public constructor (`ConnectivitySafeAreaWidget({super.key, required this.child})`) is unchanged so `main.dart` requires no modification. |
| N8 | No production file other than `connectivity_safe_area_widget.dart` is modified; `connectivity_bloc.dart`, `signalr_event_hub.dart`, `injection_container.dart`, `main.dart`, and the ARB files are untouched. |
| N9 | No new dependency is added to `pubspec.yaml`. |
| N10 | The slice ships an outside-in acceptance test driving the real `ConnectivityBloc` via a fake `EventHub` (a broadcast `StreamController<ConnectivityEvent>`), covering the five contract cases. |
| N11 | The slice ships unit tests for the pure overlay-mode resolver and widget tests (mocked `ConnectivityBloc`) for each observable overlay mode (none / reconnecting / lost). |
| N12 | The changed file passes `dart format` and `dart analyze` with no new warnings (the pre-existing `override_on_non_overriding_member` on `ConnectivityBloc.connect()` is deferred to Audit Part 4). |

## Out of scope

- SignalR/EventHub robustness (broadcast controller, init order, catch-all logging, double `jsonDecode(jsonEncode(...))`) — Audit Part 3.
- Bloc/Cubit cleanup (`ConnectivityBloc`'s false `@override connect()`, `print()` statements, constructor side effects) — Audit Part 4.
- Eager `subscribe()` at DI registration time in `injection_container.dart`.
- Renaming the misspelled `widgens/` folder or moving the file out of `lib/src/hub/`.
- Migrating the `hub` feature to ports/adapters, swapping `AppLocalizations → slang`, or renaming `*Bloc`/`*Cubit`.
- Restyling the overlay (inline `TextStyle` and dialog sizes kept for visual parity).
- Changing the connectivity state model (`ConnectivityBloc` states/events).
