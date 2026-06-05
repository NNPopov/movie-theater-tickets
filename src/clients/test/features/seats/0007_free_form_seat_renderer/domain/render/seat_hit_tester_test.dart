// Unit tests for the pure hit-test resolver (`resolveSeatAt`).
//
// Maps a point in LAYOUT space to a `SeatId?`, honouring each seat's rect
// (`x, y, w, h`) and `rotation`. A gap or an outside point resolves to null.
// Pure and widget-free.

import 'dart:math';

import 'package:flutter_test/flutter_test.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/seat_placement.dart';
import 'package:movie_theater_tickets/src/seats/domain/render/seat_hit_tester.dart';

void main() {
  // A small 2×1 grid with a one-unit gap between the two columns:
  // (1,1) at x∈[0,1], (1,2) at x∈[2,3]; both y∈[0,1].
  final twoSeats = [
    SeatPlacement(row: 1, number: 1, x: 0, y: 0),
    SeatPlacement(row: 1, number: 2, x: 2, y: 0),
  ];

  group('resolveSeatAt — axis-aligned', () {
    test('a point inside a seat rect resolves to that SeatId', () {
      expect(resolveSeatAt(const Offset(0.25, 0.75), twoSeats), (1, 1));
      expect(resolveSeatAt(const Offset(2.5, 0.5), twoSeats), (1, 2));
    });

    test('the seat centre resolves to that seat', () {
      expect(resolveSeatAt(const Offset(0.5, 0.5), twoSeats), (1, 1));
    });

    test('a point in the gap between seats resolves to null', () {
      // x = 1.5 is between the two columns (gap spans x∈(1,2)).
      expect(resolveSeatAt(const Offset(1.5, 0.5), twoSeats), isNull);
    });

    test('a point outside the bounds resolves to null', () {
      expect(resolveSeatAt(const Offset(-5, -5), twoSeats), isNull);
      expect(resolveSeatAt(const Offset(0.5, 9), twoSeats), isNull);
    });

    test('empty seat list resolves to null', () {
      expect(resolveSeatAt(const Offset(0.5, 0.5), const []), isNull);
    });
  });

  group('resolveSeatAt — variable size', () {
    test('a 2×3 seat is hit across its full extent', () {
      final big = [SeatPlacement(row: 5, number: 9, x: 0, y: 0, w: 2, h: 3)];

      expect(resolveSeatAt(const Offset(0.1, 0.1), big), (5, 9));
      expect(resolveSeatAt(const Offset(1.9, 2.9), big), (5, 9));
      // Just outside the wider extent.
      expect(resolveSeatAt(const Offset(2.1, 1), big), isNull);
    });
  });

  group('resolveSeatAt — rotation', () {
    test('rotation is honoured: hits inside the rotated rect, misses outside', () {
      // A long thin seat rotated 90° about its centre (2.5, 2.5): the un-rotated
      // rect is x∈[0,5], y∈[2,3]; rotated it occupies x∈[2,3], y∈[0,5].
      final rotated = [
        SeatPlacement(
          row: 7,
          number: 3,
          x: 0,
          y: 2,
          w: 5,
          h: 1,
          rotation: pi / 2,
        ),
      ];

      // A point inside the ROTATED footprint (tall band) hits.
      expect(resolveSeatAt(const Offset(2.5, 0.5), rotated), (7, 3));
      // The same point that would hit the UN-rotated rect now misses (proving
      // rotation is applied): (4.5, 2.5) is in the original wide rect but outside
      // the rotated band.
      expect(resolveSeatAt(const Offset(4.5, 2.5), rotated), isNull);
    });
  });

  group('resolveSeatAt — overlap', () {
    test('last match wins on overlap (defensive)', () {
      final overlapping = [
        SeatPlacement(row: 1, number: 1, x: 0, y: 0, w: 2, h: 2),
        SeatPlacement(row: 1, number: 2, x: 1, y: 1, w: 2, h: 2),
      ];

      // (1.5, 1.5) is inside both; the later placement wins.
      expect(resolveSeatAt(const Offset(1.5, 1.5), overlapping), (1, 2));
    });
  });
}
