// Outside-in acceptance test for slice 0006_hall_layout_contract.
//
// Spec: specs/features/cinema_halls/0006_hall_layout_contract/tests.md
//
// This slice has NO Cubit, NO UI and NO use-case (the documented carve-out in
// plan.md §6). Its public surface is the `SeatLayoutSource` port. So this test
// enters at `SeatLayoutSource.getLayout` with the whole slice wired real
// (adapter → synthesizer → freezed value types) and mocks only `CinemaHallRepo`
// — the legacy seam standing in for the eventual `GET …/halls/{id}/layout`
// network boundary — plus an injected `AppLogger` so the catch-all log can be
// verified. No `bloc_test`, no `getIt`: dependencies are wired by hand.
//
// Expected RED at the time of writing: the slice's contract types
// (`SeatLayout`, `SeatPlacement`, `ScreenSide`), the `SeatLayoutSource` port and
// the `BootstrapSeatLayoutSource` adapter do not exist yet, so this file fails to
// compile. That compilation failure IS the red signal; it turns green once the
// slice is implemented per plan.md.

import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

import 'package:movie_theater_tickets/core/common/app_logger.dart';
import 'package:movie_theater_tickets/core/errors/failures.dart';
import 'package:movie_theater_tickets/src/cinema_halls/data/layout/bootstrap_seat_layout_source.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/entity/cinema_hall_info.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/entity/cinema_seat.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/layout_screen.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/seat_layout.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/ports/seat_layout_source.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/repo/cinema_hall_repo.dart';

class _MockCinemaHallRepo extends Mock implements CinemaHallRepo {}

class _MockAppLogger extends Mock implements AppLogger {}

void main() {
  const hallId = 'hall-red';

  // A rectangular hall: rows 1..R, each with seats numbered 1..C, so the
  // synthesizer's x = columnIndex = (number - 1), y = rowIndex = (row - 1).
  List<List<CinemaSeat>> buildGrid(int rows, int cols) => [
    for (var r = 1; r <= rows; r++)
      [for (var n = 1; n <= cols; n++) CinemaSeat(row: r, seatNumber: n)],
  ];

  late _MockCinemaHallRepo repo;
  late _MockAppLogger logger;
  late SeatLayoutSource source;

  setUp(() {
    repo = _MockCinemaHallRepo();
    logger = _MockAppLogger();
    source = BootstrapSeatLayoutSource(repo, logger: logger);
  });

  test('Scenario 1: synthesizing a legacy hall reproduces the grid 1:1',
      () async {
    // Red 28 rows × 22 seats = 616.
    const rows = 28;
    const cols = 22;
    when(() => repo.getCinemaHallInfoById(hallId)).thenAnswer(
      (_) async => Right<Failure, CinemaHallInfo>(
        CinemaHallInfo(hallId, 'Red', buildGrid(rows, cols)),
      ),
    );

    final result = await source.getLayout(hallId);

    expect(result.isRight(), isTrue);
    final layout = result.getOrElse(() => throw StateError('expected Right'));

    expect(layout.hallId, hallId);
    expect(layout.seats.length, 616);

    for (final p in layout.seats) {
      expect(p.x, (p.number - 1).toDouble(), reason: 'x = columnIndex');
      expect(p.y, (p.row - 1).toDouble(), reason: 'y = rowIndex');
      expect(p.w, 1.0);
      expect(p.h, 1.0);
      expect(p.rotation, 0.0);
      expect(p.zoneId, isNull);
      expect(p.seatId, (p.row, p.number));
    }

    expect(layout.zones, isEmpty);

    expect(layout.screen.side, ScreenSide.top);
    expect(layout.screen.start.x, 0.0);
    expect(layout.screen.start.y, -1.0);
    expect(layout.screen.end.x, cols.toDouble());
    expect(layout.screen.end.y, -1.0);

    // bounds = (-m, -2m, C+2m, R+3m) with m = 1, C = 22, R = 28.
    expect(layout.bounds.x, -1.0);
    expect(layout.bounds.y, -2.0);
    expect(layout.bounds.width, 24.0);
    expect(layout.bounds.height, 31.0);

    verify(() => repo.getCinemaHallInfoById(hallId)).called(1);
    verifyNever(
      () => logger.e(
        any<dynamic>(),
        error: any(named: 'error'),
        stackTrace: any(named: 'stackTrace'),
      ),
    );
  });

  test('Scenario 2: an unexpected repo failure is logged and returned as Left',
      () async {
    when(() => repo.getCinemaHallInfoById(hallId)).thenThrow(Exception('boom'));

    final result = await source.getLayout(hallId);

    expect(result.isLeft(), isTrue);
    final failure = result.fold<Failure?>((f) => f, (_) => null);
    expect(failure, isA<ServerFailure>());

    verify(
      () => logger.e(
        any<dynamic>(),
        error: any(named: 'error'),
        stackTrace: any(named: 'stackTrace'),
      ),
    ).called(1);
    verify(() => repo.getCinemaHallInfoById(hallId)).called(1);
  });
}
