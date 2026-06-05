import 'entities/seat.dart';
import 'entities/seat_id.dart';

/// Builds an O(1) lookup of live seat status keyed by (row, seatNumber).
///
/// A miss (no [Seat] for a coordinate) is the caller's "empty seat" signal,
/// matching the legacy `firstWhere`-catch path. On duplicate ids, last wins
/// (defensive; the backend does not emit duplicates).
Map<SeatId, Seat> buildSeatIndex(List<Seat> seats) {
  final index = <SeatId, Seat>{};
  for (final seat in seats) {
    index[(seat.row, seat.seatNumber)] = seat;
  }
  return index;
}
