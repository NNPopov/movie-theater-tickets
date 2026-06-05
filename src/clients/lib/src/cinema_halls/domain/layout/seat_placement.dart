import 'package:freezed_annotation/freezed_annotation.dart';

import '../../../seats/domain/entities/seat_id.dart';

part 'seat_placement.freezed.dart';

part 'seat_placement.g.dart';

/// A single seat positioned in layout space.
///
/// [row] and [number] are identity/label only — [row] is NOT a structural
/// container, so variable seats-per-row, gaps, stagger and curves are just
/// different `(x, y)` entries. Zone membership is the explicit [zoneId], never
/// derived from a polygon. [seatId] is a derived getter, not a JSON field.
@freezed
abstract class SeatPlacement with _$SeatPlacement {
  const SeatPlacement._();

  factory SeatPlacement({
    required int row,
    required int number,
    required double x,
    required double y,
    @Default(1.0) double w,
    @Default(1.0) double h,
    @Default(0.0) double rotation,
    String? zoneId,
  }) = _SeatPlacement;

  factory SeatPlacement.fromJson(Map<String, Object?> json) =>
      _$SeatPlacementFromJson(json);

  /// The seat's identity, shared unchanged with slice 0005's live-status index.
  SeatId get seatId => (row, number);
}
