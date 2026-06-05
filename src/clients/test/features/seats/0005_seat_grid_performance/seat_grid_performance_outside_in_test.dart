// Outside-in acceptance test for slice 0005_seat_grid_performance.
//
// Spec: specs/features/seats/0005_seat_grid_performance/tests.md
//
// This is a *performance/parity* slice on legacy seat code. Like the precedent
// fix-slice tests (0004_connectivity_overlay_freeze_fix), its public surface is a
// WIDGET — the seat grid — not a Cubit method. So this test pumps the real
// `SeatsMovieSessionWidget` under the real `SeatBloc` + `CinemaHallInfoBloc`, drives
// seat status through the real `EventBus` (the SignalR path), and asserts externally
// observable outcomes: which colour each seat shows, that a live update recolours only
// the affected cell, that a tap routes the right shopping-cart intent, and that the
// hall scrolls sideways instead of overflowing when wider than the viewport.
//
// Boundary note: the only system boundaries faked are the two use-cases that feed the
// blocs (`GetCinemaHallInfo`, `GetSeatsByMovieSessionId`) and the `ShoppingCartCubit`
// (a `MockCubit`, used to verify tap intents). Everything else — both blocs, the real
// `EventBus`, `SeatState`/colour logic, the grid and seat widgets — is wired real.
//
// Expected RED at the time of writing: the parity scenarios (colours, live recolour,
// tap routing, empty-seat inertness) describe the *unchanged* behaviour and pass
// against the current legacy grid — they are the refactor safety net. The acceptance
// driver is the LAST scenario: today a hall wider than the viewport overflows (the grid
// is a fixed-width `Container` inside a non-scrolling `Row`), so `takeException()`
// returns a RenderFlex overflow and the scenario FAILS. It turns GREEN only once the
// hall is wrapped in a bounded horizontal scroll view per plan.md (F12 / N11).

import 'package:dartz/dartz.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

import 'package:movie_theater_tickets/core/buses/event_bus.dart';
import 'package:movie_theater_tickets/core/errors/failures.dart';
import 'package:movie_theater_tickets/core/res/app_theme.dart';
import 'package:movie_theater_tickets/l10n/gen/app_localizations.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/entity/cinema_hall_info.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/entity/cinema_seat.dart';
import 'package:movie_theater_tickets/src/cinema_halls/presentation/cubit/movie_cubit.dart';
import 'package:movie_theater_tickets/src/hub/app_events.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/entities/movie_session.dart';
import 'package:movie_theater_tickets/src/seats/domain/entities/seat.dart';
import 'package:movie_theater_tickets/src/seats/domain/usecases/get_cinema_hall_info.dart';
import 'package:movie_theater_tickets/src/seats/domain/usecases/get_seats_by_movie_session_id.dart';
import 'package:movie_theater_tickets/src/seats/presentation/cubit/seat_cubit.dart';
import 'package:movie_theater_tickets/src/seats/presentation/widgets/seat_widget.dart';
import 'package:movie_theater_tickets/src/seats/presentation/widgets/seats_movie_session_widget.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/cubit/shopping_cart_cubit.dart';

class _MockGetCinemaHallInfo extends Mock implements GetCinemaHallInfo {}

class _MockGetSeatsByMovieSessionId extends Mock
    implements GetSeatsByMovieSessionId {}

class _MockShoppingCartCubit extends Mock implements ShoppingCartCubit {}

void main() {
  const movieSessionId = 'ms-1';
  const cinemaHallId = 'hall-1';
  const myHash = 'my-hash';
  const otherHash = 'other-hash';

  final movieSession = MovieSession(
    id: movieSessionId,
    movieId: 'm-1',
    sessionDate: DateTime(2026, 1, 1, 12),
    cinemaHallId: cinemaHallId,
  );

  // A 2×2 hall geometry: (1,1) (1,2) / (2,1) (2,2).
  final grid2x2 = <List<CinemaSeat>>[
    const [
      CinemaSeat(row: 1, seatNumber: 1),
      CinemaSeat(row: 1, seatNumber: 2),
    ],
    const [
      CinemaSeat(row: 2, seatNumber: 1),
      CinemaSeat(row: 2, seatNumber: 2),
    ],
  ];

  // Initial live status: one available (grey), one taken-by-others (blue), one
  // mine-selected (greenAccent), and (2,2) deliberately ABSENT → empty (black12).
  List<Seat> initialSeats() => [
    Seat(
      row: 1,
      seatNumber: 1,
      blocked: false,
      hashId: '',
      seatStatus: SeatStatus.available,
    ),
    Seat(
      row: 1,
      seatNumber: 2,
      blocked: true,
      hashId: otherHash,
      seatStatus: SeatStatus.reserved,
    ),
    Seat(
      row: 2,
      seatNumber: 1,
      blocked: true,
      hashId: myHash,
      seatStatus: SeatStatus.selected,
    ),
  ];

  late EventBus eventBus;
  late _MockGetCinemaHallInfo getHallInfo;
  late _MockGetSeatsByMovieSessionId getSeats;
  late SeatBloc seatBloc;
  late CinemaHallInfoBloc hallBloc;
  late _MockShoppingCartCubit cart;

  void stubGeometry(List<List<CinemaSeat>> geometry) {
    when(() => getHallInfo(any())).thenAnswer(
      (_) async => Right<Failure, CinemaHallInfo>(
        CinemaHallInfo(cinemaHallId, '', geometry),
      ),
    );
  }

  setUp(() {
    eventBus = EventBus();
    getHallInfo = _MockGetCinemaHallInfo();
    getSeats = _MockGetSeatsByMovieSessionId();

    stubGeometry(grid2x2);
    when(
      () => getSeats(any()),
    ).thenAnswer((_) async => const Right<Failure, void>(null));

    seatBloc = SeatBloc(getSeats, eventBus);
    hallBloc = CinemaHallInfoBloc(getHallInfo);

    cart = _MockShoppingCartCubit();
    final cartState = ShoppingCartState.initState().copyWith(
      hashId: myHash,
      status: ShoppingCartStateStatus.update,
    );
    when(() => cart.state).thenReturn(cartState);
    when(
      () => cart.stream,
    ).thenAnswer((_) => const Stream<ShoppingCartState>.empty());
    when(() => cart.close()).thenAnswer((_) async {});
    when(
      () => cart.seatSelect(
        row: any(named: 'row'),
        seatNumber: any(named: 'seatNumber'),
        movieSessionId: any(named: 'movieSessionId'),
      ),
    ).thenAnswer((_) async {});
    when(
      () => cart.unSeatSelect(
        row: any(named: 'row'),
        seatNumber: any(named: 'seatNumber'),
        movieSessionId: any(named: 'movieSessionId'),
      ),
    ).thenAnswer((_) async {});
  });

  tearDown(() async {
    await seatBloc.close();
    await hallBloc.close();
    eventBus.dispose();
  });

  // The blocs and the EventBus are created in setUp (root zone). Delivery of the
  // geometry future, the seat-status future, and the EventBus event are async hops
  // that `tester.pump()` alone does not flush; a short `runAsync` turn drains them,
  // and the two pumps then render the resulting frame. The grid has no infinite
  // animation once loaded, but `pumpAndSettle` would hang on the loading spinner, so
  // we settle explicitly.
  Future<void> settle(WidgetTester tester) async {
    await tester.runAsync(
      () => Future<void>.delayed(const Duration(milliseconds: 30)),
    );
    await tester.pump();
    await tester.pump();
  }

  Future<void> pumpGrid(WidgetTester tester) async {
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
        theme: AppTheme.lightTheme,
        home: Scaffold(
          body: MultiBlocProvider(
            providers: [
              BlocProvider<SeatBloc>.value(value: seatBloc),
              BlocProvider<CinemaHallInfoBloc>.value(value: hallBloc),
              BlocProvider<ShoppingCartCubit>.value(value: cart),
            ],
            child: SeatsMovieSessionWidget(movieSession: movieSession),
          ),
        ),
      ),
    );
    await settle(tester);
  }

  Future<void> pushSeats(WidgetTester tester, List<Seat> seats) async {
    eventBus.send(SeatsUpdateEvent(seats));
    await settle(tester);
  }

  int countSeats(WidgetTester tester, Color color) => tester
      .widgetList<SeatWidget>(find.byType(SeatWidget))
      .where((w) => w.backgroundColor == color)
      .length;

  Finder seatByColor(Color color) => find.byWidgetPredicate(
    (w) => w is SeatWidget && w.backgroundColor == color,
  );

  testWidgets(
    'Scenario 1: each seat renders its parity colour for the given status',
    (tester) async {
      await pumpGrid(tester);
      await pushSeats(tester, initialSeats());

      expect(countSeats(tester, Colors.grey), 1); // available
      expect(countSeats(tester, Colors.blue), 1); // taken-by-others
      expect(countSeats(tester, Colors.greenAccent), 1); // mine-selected
      expect(countSeats(tester, Colors.black12), 1); // empty (index miss)
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'Scenario 2: a live status update recolours only the affected seat',
    (tester) async {
      await pumpGrid(tester);
      await pushSeats(tester, initialSeats());
      expect(countSeats(tester, Colors.grey), 1);

      // (1,1) is taken by someone else; everything else is unchanged.
      final updated = initialSeats()
        ..[0] = Seat(
          row: 1,
          seatNumber: 1,
          blocked: true,
          hashId: otherHash,
          seatStatus: SeatStatus.reserved,
        );
      await pushSeats(tester, updated);

      expect(
        countSeats(tester, Colors.grey),
        0,
      ); // the one available seat is gone
      expect(countSeats(tester, Colors.blue), 2); // (1,1) joins (1,2)
      expect(countSeats(tester, Colors.greenAccent), 1); // unchanged
      expect(countSeats(tester, Colors.black12), 1); // unchanged
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('Scenario 3: tapping a free seat routes the select intent', (
    tester,
  ) async {
    await pumpGrid(tester);
    await pushSeats(tester, initialSeats());

    await tester.tap(seatByColor(Colors.grey), warnIfMissed: false);
    await settle(tester);

    verify(
      () => cart.seatSelect(
        row: 1,
        seatNumber: 1,
        movieSessionId: movieSessionId,
      ),
    ).called(1);
    verifyNever(
      () => cart.unSeatSelect(
        row: any(named: 'row'),
        seatNumber: any(named: 'seatNumber'),
        movieSessionId: any(named: 'movieSessionId'),
      ),
    );
  });

  testWidgets(
    'Scenario 4: tapping my selected seat routes the unselect intent',
    (tester) async {
      await pumpGrid(tester);
      await pushSeats(tester, initialSeats());

      await tester.tap(seatByColor(Colors.greenAccent), warnIfMissed: false);
      await settle(tester);

      verify(
        () => cart.unSeatSelect(
          row: 2,
          seatNumber: 1,
          movieSessionId: movieSessionId,
        ),
      ).called(1);
      verifyNever(
        () => cart.seatSelect(
          row: any(named: 'row'),
          seatNumber: any(named: 'seatNumber'),
          movieSessionId: any(named: 'movieSessionId'),
        ),
      );
    },
  );

  testWidgets('Scenario 5: tapping an empty (index-miss) seat routes nothing', (
    tester,
  ) async {
    await pumpGrid(tester);
    await pushSeats(tester, initialSeats());

    await tester.tap(seatByColor(Colors.black12), warnIfMissed: false);
    await settle(tester);

    verifyNever(
      () => cart.seatSelect(
        row: any(named: 'row'),
        seatNumber: any(named: 'seatNumber'),
        movieSessionId: any(named: 'movieSessionId'),
      ),
    );
    verifyNever(
      () => cart.unSeatSelect(
        row: any(named: 'row'),
        seatNumber: any(named: 'seatNumber'),
        movieSessionId: any(named: 'movieSessionId'),
      ),
    );
  });

  testWidgets(
    'Scenario 6 (acceptance gate): a hall wider than the viewport scrolls '
    'sideways instead of overflowing',
    (tester) async {
      // A single row of 40 seats is far wider than a 300px viewport.
      final wideRow = <CinemaSeat>[
        for (var n = 1; n <= 40; n++) CinemaSeat(row: 1, seatNumber: n),
      ];
      stubGeometry(<List<CinemaSeat>>[wideRow]);

      await tester.binding.setSurfaceSize(const Size(300, 600));
      addTearDown(() => tester.binding.setSurfaceSize(null));

      await pumpGrid(tester);

      // The grid still builds its seats...
      expect(find.byType(SeatWidget), findsWidgets);
      // ...but it must do so WITHOUT a RenderFlex overflow. Today the fixed-width
      // hall container overflows the narrow viewport; after the fix it is wrapped in
      // a bounded horizontal scroll view and lays out cleanly.
      expect(tester.takeException(), isNull);
    },
  );
}
