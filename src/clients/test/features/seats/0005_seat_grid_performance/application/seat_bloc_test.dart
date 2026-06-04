// Bloc-level tests for `SeatBloc`'s derived `byId` field.
//
// The project convention is `bloc_test`, but this client only depends on
// `mocktail` (no `bloc_test`), so we drive the *real* `SeatBloc` directly:
// stub the use-case, push `SeatsUpdateEvent`s through a real `EventBus` (the
// SignalR seam), and assert the emitted state's `byId` resolves each seat
// across the load → update transition. The bloc's own logic is unchanged by
// this slice — these tests lock the derived index, not new transitions.

import 'package:dartz/dartz.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';

import 'package:movie_theater_tickets/core/buses/event_bus.dart';
import 'package:movie_theater_tickets/core/errors/failures.dart';
import 'package:movie_theater_tickets/src/hub/app_events.dart';
import 'package:movie_theater_tickets/src/seats/domain/entities/seat.dart';
import 'package:movie_theater_tickets/src/seats/domain/usecases/get_seats_by_movie_session_id.dart';
import 'package:movie_theater_tickets/src/seats/presentation/cubit/seat_cubit.dart';

class _MockGetSeats extends Mock implements GetSeatsByMovieSessionId {}

Seat _seat(
  int row,
  int seatNumber, {
  required SeatStatus status,
  bool blocked = false,
  String hashId = '',
}) {
  return Seat(
    row: row,
    seatNumber: seatNumber,
    blocked: blocked,
    hashId: hashId,
    seatStatus: status,
  );
}

void main() {
  late EventBus eventBus;
  late _MockGetSeats getSeats;
  late SeatBloc bloc;

  setUp(() {
    eventBus = EventBus();
    getSeats = _MockGetSeats();
    when(
      () => getSeats(any()),
    ).thenAnswer((_) async => const Right<Failure, void>(null));
    bloc = SeatBloc(getSeats, eventBus);
  });

  tearDown(() async {
    await bloc.close();
    eventBus.dispose();
  });

  test(
    'byId resolves each seat after load and recomputes on a status update',
    () async {
      bloc.add(const SeatEvent(movieSessionId: 'ms-1'));

      final loaded = expectLater(
        bloc.stream,
        emitsThrough(
          predicate<SeatState>(
            (s) =>
                s.status == SeatStateStatus.loaded &&
                s.byId.length == 2 &&
                s.byId[(1, 1)]?.seatStatus == SeatStatus.available &&
                s.byId[(1, 2)]?.seatStatus == SeatStatus.reserved &&
                s.byId[(9, 9)] == null,
          ),
        ),
      );
      eventBus.send(
        SeatsUpdateEvent([
          _seat(1, 1, status: SeatStatus.available),
          _seat(1, 2, status: SeatStatus.reserved, blocked: true),
        ]),
      );
      await loaded;

      // A second update (the seat (1,1) becomes selected) must be reflected in
      // the freshly-recomputed index of the next emitted state.
      final updated = expectLater(
        bloc.stream,
        emitsThrough(
          predicate<SeatState>(
            (s) => s.byId[(1, 1)]?.seatStatus == SeatStatus.selected,
          ),
        ),
      );
      eventBus.send(
        SeatsUpdateEvent([
          _seat(1, 1, status: SeatStatus.selected, blocked: true),
          _seat(1, 2, status: SeatStatus.reserved, blocked: true),
        ]),
      );
      await updated;
    },
  );

  test(
    'byId is derived and excluded from equality (props stays seats/status)',
    () {
      final seats = [_seat(1, 1, status: SeatStatus.available)];

      final a = SeatState(seats: seats, status: SeatStateStatus.loaded);
      final b = SeatState(seats: seats, status: SeatStateStatus.loaded);

      // Equal despite each carrying its own derived index.
      expect(a, equals(b));
      expect(a.byId[(1, 1)], isNotNull);
      expect(b.byId[(1, 1)], isNotNull);
    },
  );
}
