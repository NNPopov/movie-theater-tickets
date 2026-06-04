// Unit tests for the throwaway bootstrap adapter. mocktail mocks the legacy
// `CinemaHallRepo` seam and an injected `AppLogger`. Covers success delegation,
// pass-through of a known Left, and the mandatory catch-all that logs and
// returns Left(ServerFailure) on an unexpected exception (CLAUDE.md rule).

import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

import 'package:movie_theater_tickets/core/common/app_logger.dart';
import 'package:movie_theater_tickets/core/errors/failures.dart';
import 'package:movie_theater_tickets/src/cinema_halls/data/layout/bootstrap_seat_layout_source.dart';
import 'package:movie_theater_tickets/src/cinema_halls/data/layout/legacy_seat_layout_synthesizer.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/entity/cinema_hall_info.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/entity/cinema_seat.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/repo/cinema_hall_repo.dart';

class _MockCinemaHallRepo extends Mock implements CinemaHallRepo {}

class _MockAppLogger extends Mock implements AppLogger {}

List<List<CinemaSeat>> _buildGrid(int rows, int cols) => [
  for (var r = 1; r <= rows; r++)
    [for (var n = 1; n <= cols; n++) CinemaSeat(row: r, seatNumber: n)],
];

void main() {
  const hallId = 'hall-red';

  late _MockCinemaHallRepo repo;
  late _MockAppLogger logger;
  late BootstrapSeatLayoutSource source;

  setUp(() {
    repo = _MockCinemaHallRepo();
    logger = _MockAppLogger();
    source = BootstrapSeatLayoutSource(repo, logger: logger);
  });

  test(
    'returns Right(SeatLayout) matching the synthesizer (delegation)',
    () async {
      final hall = CinemaHallInfo(hallId, 'Red', _buildGrid(3, 4));
      when(
        () => repo.getCinemaHallInfoById(hallId),
      ).thenAnswer((_) async => Right<Failure, CinemaHallInfo>(hall));

      final result = await source.getLayout(hallId);

      final layout = result.getOrElse(() => throw StateError('expected Right'));
      expect(layout, synthesizeLegacyLayout(hall));
      verify(() => repo.getCinemaHallInfoById(hallId)).called(1);
      verifyNever(
        () => logger.e(
          any<dynamic>(),
          error: any(named: 'error'),
          stackTrace: any(named: 'stackTrace'),
        ),
      );
    },
  );

  test(
    'passes a known Left(Failure) through unchanged, no synthesis',
    () async {
      const failure = ServerFailure(message: 'down', statusCode: 503);
      when(
        () => repo.getCinemaHallInfoById(hallId),
      ).thenAnswer((_) async => const Left<Failure, CinemaHallInfo>(failure));

      final result = await source.getLayout(hallId);

      expect(result, const Left<Failure, dynamic>(failure));
      verify(() => repo.getCinemaHallInfoById(hallId)).called(1);
      verifyNever(
        () => logger.e(
          any<dynamic>(),
          error: any(named: 'error'),
          stackTrace: any(named: 'stackTrace'),
        ),
      );
    },
  );

  test(
    'on an unexpected exception, logs and returns Left(ServerFailure)',
    () async {
      when(
        () => repo.getCinemaHallInfoById(hallId),
      ).thenThrow(Exception('boom'));

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
    },
  );
}
