// Model invariant tests for the freezed contract value types: seatId
// derivation, field defaults, zone-membership-vs-polygon independence, value
// equality, and the JSON round-trip that locks the forward backend contract.

import 'package:flutter_test/flutter_test.dart';

import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/layout_bounds.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/layout_point.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/layout_screen.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/seat_layout.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/seat_placement.dart';
import 'package:movie_theater_tickets/src/cinema_halls/domain/layout/zone.dart';

SeatLayout _sampleLayout() => SeatLayout(
  hallId: 'hall-red',
  bounds: LayoutBounds(x: -1, y: -2, width: 24, height: 31),
  screen: Screen(
    side: ScreenSide.top,
    start: LayoutPoint(x: 0, y: -1),
    end: LayoutPoint(x: 22, y: -1),
  ),
  zones: [
    Zone(
      id: 'vip',
      label: 'VIP',
      colour: '#9C27B0',
      polygon: [
        LayoutPoint(x: 0, y: 0),
        LayoutPoint(x: 2, y: 0),
        LayoutPoint(x: 2, y: 2),
      ],
    ),
  ],
  seats: [
    SeatPlacement(row: 1, number: 1, x: 0, y: 0, zoneId: 'vip'),
    SeatPlacement(row: 1, number: 2, x: 1, y: 0),
  ],
);

void main() {
  group('SeatPlacement', () {
    test('seatId equals (row, number)', () {
      final p = SeatPlacement(row: 5, number: 7, x: 6, y: 4);
      expect(p.seatId, (5, 7));
    });

    test('w/h default to 1.0 and rotation defaults to 0.0 when omitted', () {
      final p = SeatPlacement(row: 1, number: 1, x: 0, y: 0);
      expect(p.w, 1.0);
      expect(p.h, 1.0);
      expect(p.rotation, 0.0);
      expect(p.zoneId, isNull);
    });

    test('zone membership reads from zoneId, independent of any polygon', () {
      // The seat is authored into 'vip' even though the only zone's polygon
      // does not enclose it, and a polygon that would enclose it belongs to a
      // different zone. Membership follows zoneId, never the polygon.
      final seat = SeatPlacement(
        row: 1,
        number: 1,
        x: 100,
        y: 100,
        zoneId: 'vip',
      );
      final vip = Zone(
        id: 'vip',
        label: 'VIP',
        colour: '#9C27B0',
        polygon: [
          LayoutPoint(x: 0, y: 0),
          LayoutPoint(x: 1, y: 0),
          LayoutPoint(x: 1, y: 1),
        ],
      );
      final standard = Zone(
        id: 'standard',
        label: 'Standard',
        colour: '#4CAF50',
        polygon: [
          LayoutPoint(x: 99, y: 99),
          LayoutPoint(x: 101, y: 99),
          LayoutPoint(x: 101, y: 101),
        ],
      );

      expect(seat.zoneId, 'vip');
      expect(seat.zoneId, isNot('standard'));
      // The contract models the disagreement without resolving it.
      expect(vip.id, isNot(standard.id));
    });
  });

  group('value equality', () {
    test('equal fields imply equal SeatPlacement / Zone / SeatLayout', () {
      expect(
        SeatPlacement(row: 1, number: 1, x: 0, y: 0),
        SeatPlacement(row: 1, number: 1, x: 0, y: 0),
      );
      expect(
        Zone(id: 'a', label: 'A', colour: '#fff'),
        Zone(id: 'a', label: 'A', colour: '#fff'),
      );
      expect(_sampleLayout(), _sampleLayout());
    });

    test('differing fields imply inequality', () {
      expect(
        SeatPlacement(row: 1, number: 1, x: 0, y: 0),
        isNot(SeatPlacement(row: 1, number: 2, x: 1, y: 0)),
      );
    });
  });

  group('JSON', () {
    test('fromJson/toJson round-trips a representative SeatLayout', () {
      final original = _sampleLayout();

      final restored = SeatLayout.fromJson(original.toJson());

      expect(restored, original);
    });

    test('the seatId getter is absent from the serialized JSON', () {
      final json = SeatPlacement(row: 3, number: 9, x: 8, y: 2).toJson();

      expect(json.containsKey('seatId'), isFalse);
      expect(json['row'], 3);
      expect(json['number'], 9);
    });
  });
}
