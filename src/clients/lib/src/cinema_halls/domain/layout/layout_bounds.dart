import 'package:freezed_annotation/freezed_annotation.dart';

part 'layout_bounds.freezed.dart';

part 'layout_bounds.g.dart';

/// The explicit authoring canvas extent of a hall in layout space.
///
/// This is the canvas a client fits to its viewport — NOT the seat bounding
/// box. It deliberately includes the screen and surrounding margins.
@freezed
abstract class LayoutBounds with _$LayoutBounds {
  factory LayoutBounds({
    required double x,
    required double y,
    required double width,
    required double height,
  }) = _LayoutBounds;

  factory LayoutBounds.fromJson(Map<String, Object?> json) =>
      _$LayoutBoundsFromJson(json);
}
