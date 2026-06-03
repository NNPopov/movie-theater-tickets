// Outside-in acceptance test for slice 0004_connectivity_overlay_freeze_fix.
//
// This is a *defect-fix* slice whose public surface is the APP-WIDE CONNECTIVITY
// OVERLAY (what the user sees), not a Cubit method. So — exactly like the precedent
// migration test
// 0002_navigation_auto_route_shell_migration/navigation_shell_migration_outside_in_test.dart
// (which gates at the router + widget level, not a Cubit method) — this test pumps the
// real `ConnectivitySafeAreaWidget` under a real `ConnectivityBloc`, drives connectivity
// events through a fake `EventHub`, and asserts externally observable outcomes (which
// overlay is visible, whether a transition throws). It never asserts OverlayEntry/Stack
// internals.
//
// Spec: specs/features/platform/0004_connectivity_overlay_freeze_fix/tests.md
//
// Expected RED at the time of writing: the widget is still the buggy `StatelessWidget`
// that stores `OverlayEntry` handles in `late final` fields and reassigns them. The core
// regression (Scenario 1) reproduces the original freeze: `reconnecting -> connected`
// throws `LateInitializationError`; a direct `connected` (Scenario 4/5) reads an
// unassigned `late final` and also throws. The suite turns GREEN only once the widget is
// rebuilt declaratively per plan.md.
//
// Boundary note: the only system boundary faked is `EventHub` — backed by a broadcast
// `StreamController<ConnectivityEvent>` the test controls. The broadcast controller is
// mandatory: both the bloc's stream listener and the widget's BlocListener subscribe, and
// a single-subscription stream throws "Stream has already been listened to". Everything
// else (the real `ConnectivityBloc`, `OverlayDialog`, the overlay-mode logic) is wired
// real.

import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:movie_theater_tickets/l10n/gen/app_localizations.dart';
import 'package:movie_theater_tickets/src/hub/domain/event_hub.dart';
import 'package:movie_theater_tickets/src/hub/presentation/cubit/connectivity_bloc.dart';
import 'package:movie_theater_tickets/src/hub/presentation/widgens/connectivity_safe_area_widget.dart';

/// Fake [EventHub]: its [status] stream is driven by the test. Every other member is an
/// inert no-op (routed through [noSuchMethod], which returns `Future.value()` for the
/// `Future`-returning methods the bloc never exercises in these scenarios).
class _FakeEventHub implements EventHub {
  final StreamController<ConnectivityEvent> _controller =
      StreamController<ConnectivityEvent>.broadcast();

  void emit(ConnectivityEvent event) => _controller.add(event);

  Future<void> dispose() => _controller.close();

  @override
  Stream<ConnectivityEvent> get status => _controller.stream;

  @override
  Future<void> subscribe() async {}

  @override
  dynamic noSuchMethod(Invocation invocation) => Future<void>.value();
}

void main() {
  late _FakeEventHub eventHub;
  late ConnectivityBloc bloc;

  setUp(() {
    eventHub = _FakeEventHub();
    bloc = ConnectivityBloc(eventHub);
  });

  tearDown(() async {
    await bloc.close();
    await eventHub.dispose();
  });

  // The reconnecting overlay is identified by its spinner; the connection-lost overlay by
  // its localized "Reconnect" button. The two never coexist, so these finders disambiguate
  // the three observable modes (none / reconnecting / lost).
  final spinner = find.byType(CircularProgressIndicator);
  final reconnectButton = find.widgetWithText(TextButton, 'Reconnect');

  Future<void> pumpOverlay(WidgetTester tester) async {
    await tester.pumpWidget(
      MaterialApp(
        locale: const Locale('en'),
        localizationsDelegates: const [
          AppLocalizations.delegate,
          GlobalMaterialLocalizations.delegate,
          GlobalWidgetsLocalizations.delegate,
          GlobalCupertinoLocalizations.delegate,
        ],
        supportedLocales: AppLocalizations.supportedLocales,
        home: BlocProvider<ConnectivityBloc>.value(
          value: bloc,
          child: ConnectivitySafeAreaWidget(
            child: const Text('app-content', key: Key('app_child')),
          ),
        ),
      ),
    );
    await tester.pump();
  }

  // Push an event and settle. Two pumps are needed: the bloc's stream delivery is async
  // (sync: false), so the first pump runs the bloc -> BlocListener chain (and any reactive
  // overlay insertion / setState it triggers) and the second pump renders the resulting
  // frame. `pumpAndSettle` is unusable here — the reconnecting spinner animates forever
  // and would time it out.
  Future<void> emitAndSettle(
    WidgetTester tester,
    ConnectivityEvent event,
  ) async {
    eventHub.emit(event);
    await tester.pump();
    await tester.pump();
  }

  testWidgets(
    'Scenario 1: reconnecting -> connected completes without throwing and clears the spinner',
    (tester) async {
      await pumpOverlay(tester);

      await emitAndSettle(tester, ReconnectingEvent());
      await emitAndSettle(tester, ConnectedEvent());

      // Core §2 regression: previously a LateInitializationError on the second transition.
      expect(tester.takeException(), isNull);
      expect(spinner, findsNothing);
      expect(reconnectButton, findsNothing);
    },
  );

  testWidgets('Scenario 2: reconnecting shows the reconnecting spinner', (
    tester,
  ) async {
    await pumpOverlay(tester);

    await emitAndSettle(tester, ReconnectingEvent());

    expect(tester.takeException(), isNull);
    expect(spinner, findsOneWidget);
    expect(reconnectButton, findsNothing);
  });

  testWidgets(
    'Scenario 3: cold-start disconnected (never connected) shows no connection-lost dialog',
    (tester) async {
      await pumpOverlay(tester);

      // Seed state is DisconnectedState and the app has never connected.
      await emitAndSettle(tester, DisconnectedEvent());

      expect(tester.takeException(), isNull);
      expect(reconnectButton, findsNothing);
      expect(spinner, findsNothing);
      expect(find.byKey(const Key('app_child')), findsOneWidget);
    },
  );

  testWidgets(
    'Scenario 4: connected then disconnected shows the connection-lost dialog with reconnect button',
    (tester) async {
      await pumpOverlay(tester);

      await emitAndSettle(tester, ConnectedEvent());
      await emitAndSettle(tester, DisconnectedEvent());

      expect(tester.takeException(), isNull);
      expect(reconnectButton, findsOneWidget);
      expect(spinner, findsNothing);
    },
  );

  testWidgets(
    'Scenario 5: connected after a connection-lost dialog dismisses it automatically',
    (tester) async {
      await pumpOverlay(tester);

      await emitAndSettle(tester, ConnectedEvent());
      await emitAndSettle(tester, DisconnectedEvent());

      // The lost dialog is up; an autonomous recovery must clear it.
      await emitAndSettle(tester, ConnectedEvent());

      expect(tester.takeException(), isNull);
      expect(reconnectButton, findsNothing);
      expect(spinner, findsNothing);
    },
  );
}
