import 'dart:ui';

/// Fit-to-viewport mapping between layout space (seat-pitch units, from
/// `SeatLayout.bounds`) and canvas-base space (logical px of the `CustomPaint`,
/// BEFORE the `InteractiveViewer` zoom/pan matrix).
///
/// Pure: imports `dart:ui` only (`Offset`/`Size`/`Rect`), never
/// `package:flutter`, so it stays unit-testable without pumping a widget.
class SeatLayoutTransform {
  const SeatLayoutTransform({required this.scale, required this.offset});

  /// Fits [bounds] into [canvasSize], preserving aspect ratio and centering it.
  ///
  /// A uniform scale (the smaller of the per-axis ratios) guarantees the whole
  /// bounds fits; a larger canvas yields a larger scale — the "use the extra
  /// space" property (F16/N16).
  factory SeatLayoutTransform.fit(Rect bounds, Size canvasSize) {
    final scaleX = canvasSize.width / bounds.width;
    final scaleY = canvasSize.height / bounds.height;
    final scale = scaleX < scaleY ? scaleX : scaleY;

    final scaledWidth = bounds.width * scale;
    final scaledHeight = bounds.height * scale;
    final padX = (canvasSize.width - scaledWidth) / 2;
    final padY = (canvasSize.height - scaledHeight) / 2;

    return SeatLayoutTransform(
      scale: scale,
      offset: Offset(padX - bounds.left * scale, padY - bounds.top * scale),
    );
  }

  /// Canvas px per layout unit.
  final double scale;

  /// Canvas px added after scaling (centering + bounds origin).
  final Offset offset;

  Offset layoutToCanvas(double x, double y) =>
      Offset(x * scale + offset.dx, y * scale + offset.dy);

  Offset canvasToLayout(Offset c) =>
      Offset((c.dx - offset.dx) / scale, (c.dy - offset.dy) / scale);

  @override
  bool operator ==(Object other) =>
      other is SeatLayoutTransform &&
      other.scale == scale &&
      other.offset == offset;

  @override
  int get hashCode => Object.hash(scale, offset);
}
