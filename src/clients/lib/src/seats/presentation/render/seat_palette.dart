import 'package:flutter/material.dart';

import '../../domain/entities/seat.dart';

/// What a tap on a seat should route, derived from the same logic as
/// [colorForSeat] so colour and action never diverge (N5).
enum SeatTapIntent { select, unselect, none }

/// The exact five-way palette from the legacy grid (`buildSeat`/`emptySeat`):
///
/// | condition                                                  | colour      |
/// |------------------------------------------------------------|-------------|
/// | blocked & mine & selected                                  | greenAccent |
/// | blocked & mine & not selected                              | green       |
/// | blocked & not mine                                         | blue        |
/// | otherwise (a seat exists, not blocked)                     | grey        |
/// | no seat for that id (index miss) — [seat] is `null`        | black12     |
Color colorForSeat(Seat? seat, String cartHashId) {
  if (seat == null) {
    return Colors.black12;
  }
  if (seat.blocked &&
      seat.hashId == cartHashId &&
      seat.seatStatus == SeatStatus.selected) {
    return Colors.greenAccent;
  }
  if (seat.blocked &&
      seat.hashId == cartHashId &&
      seat.seatStatus != SeatStatus.selected) {
    return Colors.green;
  }
  if (seat.blocked && seat.hashId != cartHashId) {
    return Colors.blue;
  }
  return Colors.grey;
}

/// The tap action for a seat, mirroring the colour table's last column:
/// any blocked seat (mine or other) → unselect, an available seat → select,
/// an empty/index-miss position → none (non-interactive).
SeatTapIntent tapIntentFor(Seat? seat, String cartHashId) {
  if (seat == null) {
    return SeatTapIntent.none;
  }
  if (seat.blocked) {
    return SeatTapIntent.unselect;
  }
  return SeatTapIntent.select;
}
