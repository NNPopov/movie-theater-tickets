import 'dart:math';
import 'dart:ui';

import '../../../cinema_halls/domain/layout/seat_placement.dart';
import '../entities/seat_id.dart';

/// Returns the [SeatId] whose placement rect contains [layoutPoint], or `null`.
///
/// Each seat occupies `[x, x+w] × [y, y+h]` in layout space; [SeatPlacement.rotation]
/// rotates the hit rect about the seat centre. A point in a gap between seats — or
/// outside the bounds — resolves to `null`. Last match wins on overlap (defensive).
///
/// Pure and widget-free (`dart:ui` + `dart:math` only). A linear scan is the
/// deliberate simple choice at the scale target (≤ ~1000 seats); a spatial index
/// is not warranted.
SeatId? resolveSeatAt(Offset layoutPoint, List<SeatPlacement> seats) {
  SeatId? match;
  for (final seat in seats) {
    var px = layoutPoint.dx;
    var py = layoutPoint.dy;

    if (seat.rotation != 0) {
      // Rotate the point by -rotation about the seat centre to undo the seat's
      // rotation, then test against the axis-aligned rect.
      final cx = seat.x + seat.w / 2;
      final cy = seat.y + seat.h / 2;
      final dx = px - cx;
      final dy = py - cy;
      final cosA = cos(-seat.rotation);
      final sinA = sin(-seat.rotation);
      px = cx + dx * cosA - dy * sinA;
      py = cy + dx * sinA + dy * cosA;
    }

    if (px >= seat.x &&
        px <= seat.x + seat.w &&
        py >= seat.y &&
        py <= seat.y + seat.h) {
      match = seat.seatId;
    }
  }
  return match;
}
