import 'package:freezed_annotation/freezed_annotation.dart';

import 'layout_bounds.dart';
import 'layout_screen.dart';
import 'seat_placement.dart';
import 'zone.dart';

part 'seat_layout.freezed.dart';

part 'seat_layout.g.dart';

/// The hall-layout geometry contract (layout space; no pixels, no money).
///
/// This is the shape the P5 renderer consumes and the P6 editor authors into,
/// and exactly what the eventual `GET …/halls/{id}/layout` endpoint will serve.
/// [seats] is the single source of truth for which seats exist; status and
/// price are future overlays keyed by [SeatPlacement.seatId].
@freezed
abstract class SeatLayout with _$SeatLayout {
  factory SeatLayout({
    required String hallId,
    required LayoutBounds bounds,
    required Screen screen,
    required List<SeatPlacement> seats,
    @Default(<Zone>[]) List<Zone> zones,
  }) = _SeatLayout;

  factory SeatLayout.fromJson(Map<String, Object?> json) =>
      _$SeatLayoutFromJson(json);
}
