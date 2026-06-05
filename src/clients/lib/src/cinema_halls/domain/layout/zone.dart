import 'package:freezed_annotation/freezed_annotation.dart';

import 'layout_point.dart';

part 'zone.freezed.dart';

part 'zone.g.dart';

/// A draw-only region of the hall.
///
/// [colour] is a hex string (e.g. `"#9C27B0"`), never a Flutter `Color` — the
/// contract is Flutter-free; the renderer parses it. [polygon] is draw-only and
/// may be empty. Zone membership is authored on [SeatPlacement.zoneId] and is
/// NEVER derived from this polygon.
@freezed
abstract class Zone with _$Zone {
  factory Zone({
    required String id,
    required String label,
    required String colour,
    @Default(<LayoutPoint>[]) List<LayoutPoint> polygon,
  }) = _Zone;

  factory Zone.fromJson(Map<String, Object?> json) => _$ZoneFromJson(json);
}
