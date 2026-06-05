import '../../domain/entity/cinema_hall_info.dart';
import '../../domain/layout/layout_bounds.dart';
import '../../domain/layout/layout_point.dart';
import '../../domain/layout/layout_screen.dart';
import '../../domain/layout/seat_layout.dart';
import '../../domain/layout/seat_placement.dart';

/// One seat-pitch margin around the hall and its screen, in layout space.
const double kLegacyLayoutMargin = 1.0;

/// Synthesizes a [SeatLayout] from a legacy `List<List<CinemaSeat>>` grid,
/// reproducing today's index-driven grid 1:1.
///
/// Pure and Flutter-free: no I/O, no `getIt`, no logging. The single legacy
/// default-geometry rule (slice 0006 §3.2):
/// - one [SeatPlacement] per `CinemaSeat`, `x = columnIndex`, `y = rowIndex`,
///   `w = h = 1`, `rotation = 0`, `zoneId = null`;
/// - `screen` at the top spanning the seat width;
/// - `bounds` = `(-m, -2m, C + 2m, R + 3m)` where `R` is the row count and `C`
///   the maximum seats across rows;
/// - `zones` empty.
SeatLayout synthesizeLegacyLayout(CinemaHallInfo hall) {
  const m = kLegacyLayoutMargin;

  final seats = <SeatPlacement>[];
  var maxCols = 0;
  for (var rowIndex = 0; rowIndex < hall.cinemaSeat.length; rowIndex++) {
    final innerRow = hall.cinemaSeat[rowIndex];
    if (innerRow.length > maxCols) {
      maxCols = innerRow.length;
    }
    for (var columnIndex = 0; columnIndex < innerRow.length; columnIndex++) {
      final seat = innerRow[columnIndex];
      seats.add(
        SeatPlacement(
          row: seat.row,
          number: seat.seatNumber,
          x: columnIndex.toDouble(),
          y: rowIndex.toDouble(),
        ),
      );
    }
  }

  final r = hall.cinemaSeat.length;
  final c = maxCols;

  return SeatLayout(
    hallId: hall.id,
    bounds: LayoutBounds(x: -m, y: -2 * m, width: c + 2 * m, height: r + 3 * m),
    screen: Screen(
      side: ScreenSide.top,
      start: LayoutPoint(x: 0, y: -m),
      end: LayoutPoint(x: c.toDouble(), y: -m),
    ),
    seats: seats,
  );
}
