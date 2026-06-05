import 'package:freezed_annotation/freezed_annotation.dart';

part 'layout_point.freezed.dart';

part 'layout_point.g.dart';

/// A point in layout space (seat-pitch units, origin top-left, y-down).
@freezed
abstract class LayoutPoint with _$LayoutPoint {
  factory LayoutPoint({required double x, required double y}) = _LayoutPoint;

  factory LayoutPoint.fromJson(Map<String, Object?> json) =>
      _$LayoutPointFromJson(json);
}
