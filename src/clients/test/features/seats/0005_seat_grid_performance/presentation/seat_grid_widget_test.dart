// Widget tests for the seat grid with mocked blocs (mocktail).
//
// This client has no `bloc_test`, so the three blocs are plain mocktail mocks
// with stubbed `state`/`stream`/`close` (per project convention). Each test
// renders one observable state and asserts the parity colour mapping and the
// tap → shopping-cart wiring that slice 0005 must preserve byte-for-byte.

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

import 'package:movie_theater_tickets/core/res/app_theme.dart';
import 'package:movie_theater_tickets/l10n/gen/app_localizations.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/entity/cinema_hall_info.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/entity/cinema_seat.dart';
import 'package:movie_theater_tickets/src/cinema_halls/presentation/cubit/movie_cubit.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/entities/movie_session.dart';
import 'package:movie_theater_tickets/src/seats/domain/entities/seat.dart';
import 'package:movie_theater_tickets/src/seats/presentation/cubit/seat_cubit.dart';
import 'package:movie_theater_tickets/src/seats/presentation/widgets/seat_widget.dart';
import 'package:movie_theater_tickets/src/seats/presentation/widgets/seats_movie_session_widget.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/cubit/shopping_cart_cubit.dart';

class _MockSeatBloc extends Mock implements SeatBloc {}

class _MockCinemaHallInfoBloc extends Mock implements CinemaHallInfoBloc {}

class _MockShoppingCartCubit extends Mock implements ShoppingCartCubit {}

class _FakeSeatEvent extends Fake implements SeatEvent {}

class _FakeCinemaHallInfoEvent extends Fake implements CinemaHallInfoEvent {}

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

  // (1,1) available → grey, (1,2) taken-by-others → blue,
  // (2,1) mine-selected → greenAccent, (2,2) absent → black12 (empty).
  List<Seat> seats() => [
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

  late _MockSeatBloc seatBloc;
  late _MockCinemaHallInfoBloc hallBloc;
  late _MockShoppingCartCubit cart;

  setUpAll(() {
    registerFallbackValue(_FakeSeatEvent());
    registerFallbackValue(_FakeCinemaHallInfoEvent());
  });

  setUp(() {
    seatBloc = _MockSeatBloc();
    when(() => seatBloc.add(any())).thenReturn(null);
    when(
      () => seatBloc.state,
    ).thenReturn(SeatState(seats: seats(), status: SeatStateStatus.loaded));
    when(
      () => seatBloc.stream,
    ).thenAnswer((_) => const Stream<SeatState>.empty());
    when(() => seatBloc.close()).thenAnswer((_) async {});

    hallBloc = _MockCinemaHallInfoBloc();
    when(() => hallBloc.add(any())).thenReturn(null);
    when(() => hallBloc.state).thenReturn(
      CinemaHallInfoState(
        movie: CinemaHallInfo(cinemaHallId, '', grid2x2),
        status: CinemaHallInfoStatus.completed,
      ),
    );
    when(
      () => hallBloc.stream,
    ).thenAnswer((_) => const Stream<CinemaHallInfoState>.empty());
    when(() => hallBloc.close()).thenAnswer((_) async {});

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
    await tester.pump();
  }

  int countSeats(WidgetTester tester, Color color) => tester
      .widgetList<SeatWidget>(find.byType(SeatWidget))
      .where((w) => w.backgroundColor == color)
      .length;

  Finder seatByColor(Color color) => find.byWidgetPredicate(
    (w) => w is SeatWidget && w.backgroundColor == color,
  );

  testWidgets('renders each parity colour for its status', (tester) async {
    await pumpGrid(tester);

    expect(countSeats(tester, Colors.grey), 1); // available
    expect(countSeats(tester, Colors.blue), 1); // taken-by-others
    expect(countSeats(tester, Colors.greenAccent), 1); // mine-selected
    expect(countSeats(tester, Colors.black12), 1); // empty (index miss)
  });

  testWidgets('tapping a free (grey) seat routes the select intent', (
    tester,
  ) async {
    await pumpGrid(tester);

    await tester.tap(seatByColor(Colors.grey), warnIfMissed: false);
    await tester.pump();

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
    'tapping my selected (greenAccent) seat routes the unselect intent',
    (tester) async {
      await pumpGrid(tester);

      await tester.tap(seatByColor(Colors.greenAccent), warnIfMissed: false);
      await tester.pump();

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

  testWidgets('tapping an empty (black12 / index-miss) seat routes nothing', (
    tester,
  ) async {
    await pumpGrid(tester);

    await tester.tap(seatByColor(Colors.black12), warnIfMissed: false);
    await tester.pump();

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
}
