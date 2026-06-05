// Unit tests for the layout loader Cubit (`SeatLayoutCubit`).
//
// Per project convention the client has no `bloc_test`; we drive the real cubit
// directly with a `mocktail` mock of the only collaborator — the `SeatLayoutSource`
// geometry port — and assert the emitted state sequence and held layout.

import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

import 'package:movie_theater_tickets/core/errors/failures.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/layout_bounds.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/layout_point.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/layout_screen.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/seat_layout.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/ports/seat_layout_source.dart';
import 'package:movie_theater_tickets/src/seats/presentation/cubit/seat_layout_cubit.dart';

class _MockSeatLayoutSource extends Mock implements SeatLayoutSource {}

SeatLayout _layout() => SeatLayout(
  hallId: 'hall-1',
  bounds: LayoutBounds(x: -1, y: -2, width: 4, height: 5),
  screen: Screen(
    side: ScreenSide.top,
    start: LayoutPoint(x: 0, y: -1),
    end: LayoutPoint(x: 2, y: -1),
  ),
  seats: const [],
);

void main() {
  late _MockSeatLayoutSource source;
  late SeatLayoutCubit cubit;

  setUp(() {
    source = _MockSeatLayoutSource();
    cubit = SeatLayoutCubit(source);
  });

  tearDown(() async {
    await cubit.close();
  });

  test('initial state is SeatLayoutStatus.initial with no layout', () {
    expect(cubit.state.status, SeatLayoutStatus.initial);
    expect(cubit.state.layout, isNull);
    expect(cubit.state.errorMessage, isNull);
  });

  test(
    'load success emits [loading, loaded] and holds the port layout',
    () async {
      final layout = _layout();
      when(
        () => source.getLayout(any()),
      ).thenAnswer((_) async => Right<Failure, SeatLayout>(layout));

      // Set up the expectation BEFORE triggering: emits are delivered to stream
      // listeners asynchronously, so capturing into a list after `load` returns
      // would race the final event.
      final expectation = expectLater(
        cubit.stream,
        emitsInOrder([
          predicate<SeatLayoutState>(
            (s) => s.status == SeatLayoutStatus.loading,
          ),
          predicate<SeatLayoutState>(
            (s) =>
                s.status == SeatLayoutStatus.loaded &&
                s.layout == layout &&
                s.errorMessage == null,
          ),
        ]),
      );

      await cubit.load('hall-1');
      await expectation;

      expect(cubit.state.status, SeatLayoutStatus.loaded);
      expect(cubit.state.layout, layout);
      verify(() => source.getLayout('hall-1')).called(1);
    },
  );

  test('load failure emits [loading, error] with the failure message; '
      'layout stays null', () async {
    const failure = ServerFailure(message: 'boom', statusCode: 500);
    when(
      () => source.getLayout(any()),
    ).thenAnswer((_) async => Left<Failure, SeatLayout>(failure));

    final expectation = expectLater(
      cubit.stream,
      emitsInOrder([
        predicate<SeatLayoutState>((s) => s.status == SeatLayoutStatus.loading),
        predicate<SeatLayoutState>(
          (s) =>
              s.status == SeatLayoutStatus.error &&
              s.layout == null &&
              s.errorMessage == failure.errorMessage,
        ),
      ]),
    );

    await cubit.load('hall-1');
    await expectation;

    expect(cubit.state.status, SeatLayoutStatus.error);
    expect(cubit.state.layout, isNull);
    expect(cubit.state.errorMessage, failure.errorMessage);
  });
}
