import 'package:freezed_annotation/freezed_annotation.dart';

import 'layout_point.dart';

part 'layout_screen.freezed.dart';

part 'layout_screen.g.dart';

/// Which edge of the hall the screen sits on.
enum ScreenSide { top, bottom, left, right }

/// The cinema screen the seats face: a segment plus which edge it lies on.
@freezed
abstract class Screen with _$Screen {
  factory Screen({
    required ScreenSide side,
    required LayoutPoint start,
    required LayoutPoint end,
  }) = _Screen;

  factory Screen.fromJson(Map<String, Object?> json) => _$ScreenFromJson(json);
}
