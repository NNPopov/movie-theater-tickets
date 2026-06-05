// Unit tests for the pure status-index seam (`buildSeatIndex`).
//
// This is the deep, Flutter-free module of slice 0005: it turns the live
// `List<Seat>` into an O(1) `Map<SeatId, Seat>` keyed by (row, seatNumber).
// A lookup miss returns null — the "empty seat" signal that reproduces the
// legacy `firstWhere`-catch path.

import 'package:flutter_test/flutter_test.dart';

import 'package:movie_theater_tickets/src/seats/domain/entities/seat.dart';
import 'package:movie_theater_tickets/src/seats/domain/seat_index.dart';

Seat _seat(
  int row,
  int seatNumber, {
  SeatStatus status = SeatStatus.available,
}) {
  return Seat(
    row: row,
    seatNumber: seatNumber,
    blocked: false,
    hashId: '',
    seatStatus: status,
  );
}

void main() {
  group('buildSeatIndex', () {
    test(
      'maps each seat by (row, seatNumber); a hit returns the right seat',
      () {
        final a = _seat(1, 1, status: SeatStatus.available);
        final b = _seat(1, 2, status: SeatStatus.reserved);
        final c = _seat(2, 1, status: SeatStatus.selected);

        final index = buildSeatIndex([a, b, c]);

        expect(index.length, 3);
        expect(index[(1, 1)], same(a));
        expect(index[(1, 2)], same(b));
        expect(index[(2, 1)], same(c));
      },
    );

    test('a lookup miss returns null (the empty-seat signal)', () {
      final index = buildSeatIndex([_seat(1, 1)]);

      expect(index[(9, 9)], isNull);
    });

    test('an empty input list produces an empty map', () {
      expect(buildSeatIndex(const []), isEmpty);
    });

    test('duplicate ids: last wins, no throw (defensive)', () {
      final first = _seat(1, 1, status: SeatStatus.available);
      final second = _seat(1, 1, status: SeatStatus.sold);

      final index = buildSeatIndex([first, second]);

      expect(index.length, 1);
      expect(index[(1, 1)], same(second));
    });
  });
}
