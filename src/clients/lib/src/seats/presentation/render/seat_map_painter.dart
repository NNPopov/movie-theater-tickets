import 'package:flutter/material.dart';

import '../../../cinema_halls/domain/layout/seat_layout.dart';
import '../../domain/entities/seat.dart';
import '../../domain/entities/seat_id.dart';
import '../../domain/render/seat_layout_transform.dart';
import 'seat_palette.dart';

/// Draws the whole hall from [SeatLayout] geometry + the live status index.
///
/// Per ADR 0005 §6 (and the PRD's documented deviation, recorded in
/// `.claude/decisions/0007_free_form_seat_renderer.md`): a painter has no per-seat
/// widgets, so it reads the whole [byId] map directly. The O(1) per-seat lookup and
/// single map-level subscription are preserved; "only the affected seat changes" is
/// satisfied visually (only that seat's fill differs between frames) rather than via
/// per-widget rebuild isolation. A whole-canvas redraw on a status emit is cheap at
/// the scale target.
///
/// Pixels are intentionally NOT asserted by tests; correctness is guaranteed through
/// the pure modules ([SeatLayoutTransform], `resolveSeatAt`, `colorForSeat`) and the
/// behavioural taps. `zones` and price are ignored — status only (N14).
class SeatMapPainter extends CustomPainter {
  SeatMapPainter({
    required this.layout,
    required this.transform,
    required this.byId,
    required this.cartHashId,
  });

  final SeatLayout layout;
  final SeatLayoutTransform transform;
  final Map<SeatId, Seat> byId;
  final String cartHashId;

  @override
  void paint(Canvas canvas, Size size) {
    _paintScreen(canvas);
    _paintSeats(canvas);
  }

  void _paintScreen(Canvas canvas) {
    final start = transform.layoutToCanvas(
      layout.screen.start.x,
      layout.screen.start.y,
    );
    final end = transform.layoutToCanvas(
      layout.screen.end.x,
      layout.screen.end.y,
    );
    canvas.drawLine(
      start,
      end,
      Paint()
        ..color = Colors.grey.shade600
        ..strokeWidth = 4
        ..strokeCap = StrokeCap.round,
    );
  }

  void _paintSeats(Canvas canvas) {
    for (final placement in layout.seats) {
      final seat = byId[placement.seatId];
      final fill = colorForSeat(seat, cartHashId);

      final topLeft = transform.layoutToCanvas(placement.x, placement.y);
      final w = placement.w * transform.scale;
      final h = placement.h * transform.scale;
      final rect = Rect.fromLTWH(topLeft.dx, topLeft.dy, w, h);

      canvas.save();
      if (placement.rotation != 0) {
        final center = transform.layoutToCanvas(
          placement.x + placement.w / 2,
          placement.y + placement.h / 2,
        );
        canvas
          ..translate(center.dx, center.dy)
          ..rotate(placement.rotation)
          ..translate(-center.dx, -center.dy);
      }

      final body = rect.deflate(w * 0.08);
      canvas.drawRRect(
        RRect.fromRectAndRadius(body, const Radius.circular(2)),
        Paint()..color = fill,
      );
      // Seat numbers are re-laid-out each paint with a TextPainter, so they stay
      // crisp at any zoom (the core reason CustomPaint beat Stack+Positioned).
      if (seat != null) {
        _paintNumber(canvas, placement.number.toString(), body);
      }
      canvas.restore();
    }
  }

  void _paintNumber(Canvas canvas, String text, Rect rect) {
    final tp = TextPainter(
      text: TextSpan(
        text: text,
        style: TextStyle(
          color: Colors.black87,
          fontSize: (rect.height * 0.5).clamp(6.0, 18.0).toDouble(),
        ),
      ),
      textAlign: TextAlign.center,
      textDirection: TextDirection.ltr,
    )..layout(maxWidth: rect.width);

    tp.paint(
      canvas,
      Offset(
        rect.left + (rect.width - tp.width) / 2,
        rect.top + (rect.height - tp.height) / 2,
      ),
    );
  }

  @override
  bool shouldRepaint(SeatMapPainter old) =>
      !identical(byId, old.byId) ||
      cartHashId != old.cartHashId ||
      transform != old.transform ||
      !identical(layout, old.layout);
}
