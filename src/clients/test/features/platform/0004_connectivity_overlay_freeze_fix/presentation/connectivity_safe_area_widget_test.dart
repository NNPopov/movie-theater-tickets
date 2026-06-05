// Widget tests for `ConnectivitySafeAreaWidget` (slice
// 0004_connectivity_overlay_freeze_fix), one per observable overlay mode
// (none / reconnecting / lost).
//
// The cubit is driven by a lightweight stub `ConnectivityBloc` (the same idiom as the
// 0002 migration test: `extends Cubit<ConnectivityState> implements ConnectivityBloc`),
// not bloc_test's `MockCubit`/`whenListen` — this project does not depend on `bloc_test`.
// The stub is created inside the test body (in the per-test async zone) so a plain
// `tester.pump()` flushes the state delivery. Because `BlocListener` reacts only to state
// *changes* (never the seed), the reconnecting and lost cases drive an explicit transition
// from the `DisconnectedState` seed.

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:movie_theater_tickets/l10n/gen/app_localizations.dart';
import 'package:movie_theater_tickets/src/hub/presentation/cubit/connectivity_bloc.dart';
import 'package:movie_theater_tickets/src/hub/presentation/widgens/connectivity_safe_area_widget.dart';

/// Inert stub: a real [Cubit] whose state the test drives via [push]. Implements
/// [ConnectivityBloc] so it satisfies the `BlocProvider<ConnectivityBloc>` type; its
/// [connect] is a no-op (the widget tests assert rendering, not reconnect behaviour).
class _StubConnectivityBloc extends Cubit<ConnectivityState>
    implements ConnectivityBloc {
  _StubConnectivityBloc() : super(DisconnectedState());

  void push(ConnectivityState state) => emit(state);

  @override
  Future<void> connect() async {}
}

void main() {
  late _StubConnectivityBloc bloc;

  setUp(() => bloc = _StubConnectivityBloc());
  tearDown(() => bloc.close());

  final spinner = find.byType(CircularProgressIndicator);
  final reconnectButton = find.widgetWithText(TextButton, 'Reconnect');
  final childFinder = find.byKey(const Key('app_child'));

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

  // The stub bloc is created in `setUp`, i.e. outside the per-test fake-async zone, so its
  // state stream lives in the root zone. A single `runAsync` turn of the real event loop
  // flushes the bloc -> BlocListener -> setState chain that a plain `pump()` would miss;
  // the pump then renders the frame. (Same rationale as the outside-in test's settling.)
  Future<void> pushAndSettle(
    WidgetTester tester,
    ConnectivityState state,
  ) async {
    bloc.push(state);
    await tester.runAsync(
      () => Future<void>.delayed(const Duration(milliseconds: 5)),
    );
    await tester.pump();
  }

  testWidgets('mode none: seed Disconnected shows no overlay, child visible', (
    tester,
  ) async {
    await pumpOverlay(tester);

    expect(spinner, findsNothing);
    expect(reconnectButton, findsNothing);
    expect(childFinder, findsOneWidget);
  });

  testWidgets('mode reconnecting: shows the spinner and reconnecting header', (
    tester,
  ) async {
    await pumpOverlay(tester);

    await pushAndSettle(tester, ReconnectingState());

    expect(spinner, findsOneWidget);
    expect(
      find.text('The connection to the server has been lost! Reconnecting!'),
      findsOneWidget,
    );
    expect(reconnectButton, findsNothing);
  });

  testWidgets(
    'mode lost: Connected then Disconnected shows the lost dialog + reconnect button',
    (tester) async {
      await pumpOverlay(tester);

      await pushAndSettle(tester, ConnectedState());
      await pushAndSettle(tester, DisconnectedState());

      expect(reconnectButton, findsOneWidget);
      expect(
        find.text(
          'The connection to the server has been lost! Please try reconnecting.',
        ),
        findsOneWidget,
      );
      expect(spinner, findsNothing);
    },
  );
}
