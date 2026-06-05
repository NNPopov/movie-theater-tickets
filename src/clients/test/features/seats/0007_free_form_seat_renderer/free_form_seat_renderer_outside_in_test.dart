// Outside-in acceptance test for slice 0007_free_form_seat_renderer.
//
// Spec: specs/features/seats/0007_free_form_seat_renderer/tests.md
//
// Like slice 0005, this slice's contract is *behavioural parity of the rendered
// hall under live updates* — not a Cubit→Dio round-trip. The new renderer is a
// single `CustomPaint` inside an `InteractiveViewer`; it has NO per-seat widgets,
// and (per prd.md) its pixels are NOT asserted. So this test pumps the real
// `SeatMapView` under the real `SeatLayoutCubit` (geometry) + real `SeatBloc`
// (status, fed through the real `EventBus` — the SignalR seam), aims `tester.tapAt`
// at each seat's canvas centre — computed with the public, pure `SeatLayoutTransform`
// at the initial (identity) zoom — and asserts the routed shopping-cart intent.
//
// Recolour is proven RENDER-AGNOSTICALLY: after a live status event flips a free
// seat to mine-selected, tapping that same seat routes a DIFFERENT intent (status
// reached the renderer), while an untouched seat routes the SAME as before
// (per-seat isolation) — no pixel is read.
//
// Boundaries faked: the geometry port (`SeatLayoutSource`), the status use-case
// (`GetSeatsByMovieSessionId`), and `ShoppingCartCubit` (a mock, used to verify
// tap intents). Everything else — both state holders, the real `EventBus`, the
// pure transform/hit-test/palette, `SeatMapView`/`SeatMapPainter` — is wired real.
//
// Expected RED at the time of writing: the slice's implementation does not exist
// yet (`SeatLayoutCubit`, `SeatLayoutTransform`, `SeatMapView`), so this file fails
// to compile. That compilation failure is the red signal. It turns green only once
// the renderer, the loader cubit, and the pure modules are implemented per plan.md.

import 'dart:ui';

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
import 'package:movie_theater_tickets/src/cinema_halls/data/layout/legacy_seat_layout_synthesizer.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/entity/cinema_hall_info.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/entity/cinema_seat.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/seat_layout.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/ports/seat_layout_source.dart';
import 'package:movie_theater_tickets/src/hub/app_events.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/entities/movie_session.dart';
import 'package:movie_theater_tickets/src/seats/domain/entities/seat.dart';
import 'package:movie_theater_tickets/src/seats/domain/render/seat_layout_transform.dart';
import 'package:movie_theater_tickets/src/seats/domain/usecases/get_seats_by_movie_session_id.dart';
import 'package:movie_theater_tickets/src/seats/presentation/cubit/seat_cubit.dart';
import 'package:movie_theater_tickets/src/seats/presentation/cubit/seat_layout_cubit.dart';
import 'package:movie_theater_tickets/src/seats/presentation/widgets/seat_map_view.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/cubit/shopping_cart_cubit.dart';

class _MockSeatLayoutSource extends Mock implements SeatLayoutSource {}

class _MockGetSeatsByMovieSessionId extends Mock
    implements GetSeatsByMovieSessionId {}

class _MockShoppingCartCubit extends Mock implements ShoppingCartCubit {}

void main() {
  const movieSessionId = 'ms-1';
  const cinemaHallId = 'hall-1';
  const myHash = 'my-hash';
  const otherHash = 'other-hash';
  const canvasKey = Key('seat-canvas');

  final movieSession = MovieSession(
    id: movieSessionId,
    movieId: 'm-1',
    sessionDate: DateTime(2026, 1, 1, 12),
    cinemaHallId: cinemaHallId,
  );

  // A 2×2 hall: (1,1) (1,2) / (2,1) (2,2). Synthesised to a faithful legacy
  // SeatLayout (bounds LTWH(-1,-2,4,5); seats at (0,0),(1,0),(0,1),(1,1); screen
  // on top) — this also exercises the legacy-render path (F18).
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

  // Build the layout from the real synthesiser (kept as a function so the bounds
  // are taken straight from production code).
  SeatLayout buildLayout() =>
      synthesizeLegacyLayout(CinemaHallInfo(cinemaHallId, '', grid2x2));

  Rect boundsRectOf(SeatLayout l) =>
      Rect.fromLTWH(l.bounds.x, l.bounds.y, l.bounds.width, l.bounds.height);

  // Initial live status: (1,1) available→grey, (1,2) taken-by-others→blue,
  // (2,1) mine-selected→greenAccent, (2,2) ABSENT → status-miss → black12.
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
  late _MockSeatLayoutSource layoutSource;
  late _MockGetSeatsByMovieSessionId getSeats;
  late SeatBloc seatBloc;
  late SeatLayoutCubit seatLayoutCubit;
  late _MockShoppingCartCubit cart;

  setUp(() {
    eventBus = EventBus();
    layoutSource = _MockSeatLayoutSource();
    getSeats = _MockGetSeatsByMovieSessionId();

    when(
      () => layoutSource.getLayout(any()),
    ).thenAnswer((_) async => Right<Failure, SeatLayout>(buildLayout()));
    when(
      () => getSeats(any()),
    ).thenAnswer((_) async => const Right<Failure, void>(null));

    seatBloc = SeatBloc(getSeats, eventBus);
    seatLayoutCubit = SeatLayoutCubit(layoutSource);

    cart = _MockShoppingCartCubit();
    final cartState = ShoppingCartState.initState().copyWith(
      hashId: myHash,
      status: ShoppingCartStateStatus.update,
    );
    when(() => cart.state).thenReturn(cartState);
    when(() => cart.hashId).thenReturn(myHash);
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
    await seatLayoutCubit.close();
    eventBus.dispose();
  });

  // Async hops (layout future, status future, EventBus event) are not flushed by
  // pump() alone; a short runAsync turn drains them, then two pumps render the
  // frame. pumpAndSettle would hang on the loading view, so we settle explicitly.
  Future<void> settle(WidgetTester tester) async {
    await tester.runAsync(
      () => Future<void>.delayed(const Duration(milliseconds: 30)),
    );
    await tester.pump();
    await tester.pump();
  }

  Future<void> pumpView(WidgetTester tester) async {
    await tester.binding.setSurfaceSize(const Size(400, 600));
    addTearDown(() => tester.binding.setSurfaceSize(null));

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
          body: SizedBox(
            key: canvasKey,
            width: 400,
            height: 600,
            child: MultiBlocProvider(
              providers: [
                BlocProvider<SeatBloc>.value(value: seatBloc),
                BlocProvider<SeatLayoutCubit>.value(value: seatLayoutCubit),
                BlocProvider<ShoppingCartCubit>.value(value: cart),
              ],
              child: SeatMapView(movieSession: movieSession),
            ),
          ),
        ),
      ),
    );
    // The screen's provider kicks the layout load in production; drive it here.
    seatLayoutCubit.load(cinemaHallId);
    await settle(tester);
  }

  Future<void> pushSeats(WidgetTester tester, List<Seat> seats) async {
    eventBus.send(SeatsUpdateEvent(seats));
    await settle(tester);
  }

  // Canvas centre of the seat whose placement origin is (x, y) in layout space,
  // computed with the SAME public transform production uses — render-agnostic.
  Offset seatCenter(WidgetTester tester, double x, double y) {
    final canvasRect = tester.getRect(find.byKey(canvasKey));
    final transform = SeatLayoutTransform.fit(
      boundsRectOf(buildLayout()),
      canvasRect.size,
    );
    return canvasRect.topLeft + transform.layoutToCanvas(x + 0.5, y + 0.5);
  }

  // An arbitrary layout point (no half-seat offset) — used to aim at the empty
  // screen/margin band where no seat placement exists.
  Offset layoutPointInCanvas(WidgetTester tester, double x, double y) {
    final canvasRect = tester.getRect(find.byKey(canvasKey));
    final transform = SeatLayoutTransform.fit(
      boundsRectOf(buildLayout()),
      canvasRect.size,
    );
    return canvasRect.topLeft + transform.layoutToCanvas(x, y);
  }

  testWidgets(
    'Scenario 1: taps route the right intent; a live update changes only the '
    'affected seat',
    (tester) async {
      await pumpView(tester);
      await pushSeats(tester, initialSeats());

      // (1,1) free → select; (2,1) mine → unselect.
      await tester.tapAt(seatCenter(tester, 0, 0)); // seat (1,1) at (x0,y0)
      await settle(tester);
      await tester.tapAt(seatCenter(tester, 0, 1)); // seat (2,1) at (x0,y1)
      await settle(tester);

      // (2,2): geometry present but status-absent → non-interactive.
      await tester.tapAt(seatCenter(tester, 1, 1)); // seat (2,2) at (x1,y1)
      await settle(tester);

      // A gap: the screen/margin band at layout (0.5,-1) → no seat placement.
      await tester.tapAt(layoutPointInCanvas(tester, 0.5, -1));
      await settle(tester);

      verify(
        () => cart.seatSelect(
          row: 1,
          seatNumber: 1,
          movieSessionId: movieSessionId,
        ),
      ).called(1);
      verify(
        () => cart.unSeatSelect(
          row: 2,
          seatNumber: 1,
          movieSessionId: movieSessionId,
        ),
      ).called(1);
      // (2,2) and the gap routed nothing.
      verifyNever(
        () => cart.seatSelect(
          row: 2,
          seatNumber: 2,
          movieSessionId: any(named: 'movieSessionId'),
        ),
      );
      verifyNever(
        () => cart.unSeatSelect(
          row: 2,
          seatNumber: 2,
          movieSessionId: any(named: 'movieSessionId'),
        ),
      );

      // Live update: (1,1) becomes mine-selected — the only seat that changes.
      final updated = initialSeats()
        ..[0] = Seat(
          row: 1,
          seatNumber: 1,
          blocked: true,
          hashId: myHash,
          seatStatus: SeatStatus.selected,
        );
      await pushSeats(tester, updated);

      // (1,1) now routes UNSELECT (status reached the renderer)...
      await tester.tapAt(seatCenter(tester, 0, 0));
      await settle(tester);
      // ...while the untouched (2,1) still routes unselect as before (isolation).
      await tester.tapAt(seatCenter(tester, 0, 1));
      await settle(tester);

      verify(
        () => cart.unSeatSelect(
          row: 1,
          seatNumber: 1,
          movieSessionId: movieSessionId,
        ),
      ).called(1);
      verify(
        () => cart.unSeatSelect(
          row: 2,
          seatNumber: 1,
          movieSessionId: movieSessionId,
        ),
      ).called(1); // the SECOND (2,1) tap; first was verified above
      // (1,1) never routed a select after it became mine-selected.
      verifyNever(
        () => cart.seatSelect(
          row: 1,
          seatNumber: 1,
          movieSessionId: any(named: 'movieSessionId'),
        ),
      );

      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'Scenario 2: a layout-load failure shows an error and routes nothing',
    (tester) async {
      when(() => layoutSource.getLayout(any())).thenAnswer(
        (_) async => Left<Failure, SeatLayout>(
          ServerFailure(message: 'boom', statusCode: 500),
        ),
      );

      await pumpView(tester);

      // Attempt a tap in the hall area — nothing should be routed.
      await tester.tapAt(const Offset(200, 300));
      await settle(tester);

      expect(seatLayoutCubit.state.status, SeatLayoutStatus.error);
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
      expect(tester.takeException(), isNull);
    },
  );
}
