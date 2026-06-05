// Unit tests for the pure status→colour palette and the shared tap-intent
// classifier (`colorForSeat` / `tapIntentFor`).
//
// Reproduces the legacy `buildSeat`/`emptySeat` five-way palette byte-for-byte,
// and proves colour and action are derived from the same logic so they can never
// diverge (N5).

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:movie_theater_tickets/src/seats/domain/entities/seat.dart';
import 'package:movie_theater_tickets/src/seats/presentation/render/seat_palette.dart';

const myHash = 'my-hash';
const otherHash = 'other-hash';

Seat _seat({
  required bool blocked,
  required String hashId,
  required SeatStatus status,
}) {
  return Seat(
    row: 1,
    seatNumber: 1,
    blocked: blocked,
    hashId: hashId,
    seatStatus: status,
  );
}

void main() {
  group('colorForSeat', () {
    test('mine-selected → greenAccent', () {
      final seat = _seat(
        blocked: true,
        hashId: myHash,
        status: SeatStatus.selected,
      );
      expect(colorForSeat(seat, myHash), Colors.greenAccent);
    });

    test('mine-blocked (not selected) → green', () {
      final seat = _seat(
        blocked: true,
        hashId: myHash,
        status: SeatStatus.reserved,
      );
      expect(colorForSeat(seat, myHash), Colors.green);
    });

    test('taken-by-others → blue', () {
      final seat = _seat(
        blocked: true,
        hashId: otherHash,
        status: SeatStatus.reserved,
      );
      expect(colorForSeat(seat, myHash), Colors.blue);
    });

    test('available (not blocked) → grey', () {
      final seat = _seat(
        blocked: false,
        hashId: '',
        status: SeatStatus.available,
      );
      expect(colorForSeat(seat, myHash), Colors.grey);
    });

    test('null seat (index miss / empty) → black12', () {
      expect(colorForSeat(null, myHash), Colors.black12);
    });
  });

  group('tapIntentFor agrees with the palette', () {
    test('mine-selected → unselect', () {
      final seat = _seat(
        blocked: true,
        hashId: myHash,
        status: SeatStatus.selected,
      );
      expect(tapIntentFor(seat, myHash), SeatTapIntent.unselect);
    });

    test('mine-blocked (not selected) → unselect', () {
      final seat = _seat(
        blocked: true,
        hashId: myHash,
        status: SeatStatus.reserved,
      );
      expect(tapIntentFor(seat, myHash), SeatTapIntent.unselect);
    });

    test('taken-by-others → unselect (preserves legacy behaviour)', () {
      final seat = _seat(
        blocked: true,
        hashId: otherHash,
        status: SeatStatus.reserved,
      );
      expect(tapIntentFor(seat, myHash), SeatTapIntent.unselect);
    });

    test('available → select', () {
      final seat = _seat(
        blocked: false,
        hashId: '',
        status: SeatStatus.available,
      );
      expect(tapIntentFor(seat, myHash), SeatTapIntent.select);
    });

    test('null seat (index miss / empty) → none', () {
      expect(tapIntentFor(null, myHash), SeatTapIntent.none);
    });
  });
}
