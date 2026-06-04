// Mapping-parity unit tests for the pure legacy-geometry synthesizer — the
// centre of gravity of slice 0006. These lock the "reproduces today's grid 1:1"
// promise: the §3.2 rule, the bounds/screen formulas, the defensive edges
// (empty, ragged), and the three seeded shapes' seat counts (616 / 378 / 180).

import 'package:flutter_test/flutter_test.dart';

import 'package:movie_theater_tickets/src/cinema_halls/data/layout/legacy_seat_layout_synthesizer.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/entity/cinema_hall_info.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/entity/cinema_seat.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/layout_screen.dart';

const double m = kLegacyLayoutMargin;

/// A rectangular hall: rows 1..R, each with seats numbered 1..C.
List<List<CinemaSeat>> buildGrid(int rows, int cols) => [
  for (var r = 1; r <= rows; r++)
    [for (var n = 1; n <= cols; n++) CinemaSeat(row: r, seatNumber: n)],
];

void main() {
  group('synthesizeLegacyLayout', () {
    test('a rectangular hall maps to the expected flat SeatPlacement list', () {
      const rows = 3;
      const cols = 4;
      final hall = CinemaHallInfo('hall-x', 'X', buildGrid(rows, cols));

      final layout = synthesizeLegacyLayout(hall);

      expect(layout.hallId, 'hall-x');
      expect(layout.seats.length, rows * cols);

      for (final p in layout.seats) {
        // x = columnIndex = number - 1, y = rowIndex = row - 1.
        expect(p.x, (p.number - 1).toDouble(), reason: 'x = columnIndex');
        expect(p.y, (p.row - 1).toDouble(), reason: 'y = rowIndex');
        expect(p.w, 1.0);
        expect(p.h, 1.0);
        expect(p.rotation, 0.0);
        expect(p.zoneId, isNull);
      }

      expect(layout.zones, isEmpty);
    });

    test('SeatId identity round-trips for every seat', () {
      final hall = CinemaHallInfo('hall-x', 'X', buildGrid(3, 4));

      final layout = synthesizeLegacyLayout(hall);

      for (final p in layout.seats) {
        expect(p.seatId, (p.row, p.number));
      }
    });

    test(
      'bounds equals (-m, -2m, C+2m, R+3m) — explicit, not the seat bbox',
      () {
        const rows = 3;
        const cols = 4;
        final hall = CinemaHallInfo('hall-x', 'X', buildGrid(rows, cols));

        final bounds = synthesizeLegacyLayout(hall).bounds;

        expect(bounds.x, -m);
        expect(bounds.y, -2 * m);
        expect(bounds.width, cols + 2 * m);
        expect(bounds.height, rows + 3 * m);
      },
    );

    test(
      'screen is top, spanning the seat width: start (0,-m), end (C,-m)',
      () {
        const cols = 4;
        final hall = CinemaHallInfo('hall-x', 'X', buildGrid(3, cols));

        final screen = synthesizeLegacyLayout(hall).screen;

        expect(screen.side, ScreenSide.top);
        expect(screen.start.x, 0.0);
        expect(screen.start.y, -m);
        expect(screen.end.x, cols.toDouble());
        expect(screen.end.y, -m);
      },
    );

    test('an empty hall maps to seats: [] with C=0, R=0, no throw', () {
      final hall = CinemaHallInfo('hall-empty', 'Empty', const []);

      final layout = synthesizeLegacyLayout(hall);

      expect(layout.seats, isEmpty);
      expect(layout.zones, isEmpty);
      // C = 0, R = 0.
      expect(layout.bounds.x, -m);
      expect(layout.bounds.y, -2 * m);
      expect(layout.bounds.width, 2 * m);
      expect(layout.bounds.height, 3 * m);
      expect(layout.screen.start.x, 0.0);
      expect(layout.screen.end.x, 0.0);
    });

    test(
      'a ragged inner list maps each seat by its own indices, no padding',
      () {
        // Row 1 has 3 seats, row 2 has 1 seat → C = max(3, 1) = 3, R = 2.
        final grid = [
          [
            const CinemaSeat(row: 1, seatNumber: 1),
            const CinemaSeat(row: 1, seatNumber: 2),
            const CinemaSeat(row: 1, seatNumber: 3),
          ],
          [const CinemaSeat(row: 2, seatNumber: 1)],
        ];
        final hall = CinemaHallInfo('hall-ragged', 'Ragged', grid);

        final layout = synthesizeLegacyLayout(hall);

        expect(layout.seats.length, 4);
        // The lone seat in row 2 sits at columnIndex 0, rowIndex 1.
        final lone = layout.seats.firstWhere((p) => p.row == 2);
        expect(lone.x, 0.0);
        expect(lone.y, 1.0);
        // C = 3, R = 2.
        expect(layout.bounds.width, 3 + 2 * m);
        expect(layout.bounds.height, 2 + 3 * m);
        expect(layout.screen.end.x, 3.0);
      },
    );

    group('the three seeded shapes lock seat counts and extents', () {
      // Orientation confirmed against the outside-in fixture: Red is 28 rows ×
      // 22 cols, so dims are rows × cols.
      void expectShape(String id, int rows, int cols, int count) {
        final layout = synthesizeLegacyLayout(
          CinemaHallInfo(id, id, buildGrid(rows, cols)),
        );
        expect(layout.seats.length, count, reason: '$id seat count');
        expect(layout.bounds.width, cols + 2 * m, reason: '$id width');
        expect(layout.bounds.height, rows + 3 * m, reason: '$id height');
      }

      test('Red 28×22 = 616', () => expectShape('hall-red', 28, 22, 616));
      test('Black 21×18 = 378', () => expectShape('hall-black', 21, 18, 378));
      test('White 15×12 = 180', () => expectShape('hall-white', 15, 12, 180));
    });
  });
}
