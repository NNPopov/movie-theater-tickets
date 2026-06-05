// Unit tests for the pure fit-to-viewport transform (`SeatLayoutTransform`).
//
// The bulk of the renderer's geometric value: a uniform-scale, centred mapping
// between layout space (`SeatLayout.bounds`) and canvas-base space, with a tested
// round-trip inverse. Pure (`dart:ui` only) — no widget pump.

import 'dart:ui';

import 'package:flutter_test/flutter_test.dart';
import 'package:movie_theater_tickets/src/seats/domain/render/seat_layout_transform.dart';

void main() {
  // A non-trivial bounds with a negative origin (matches the legacy synthesiser's
  // `LTWH(-m, -2m, ...)`), so origin handling is actually exercised.
  final bounds = Rect.fromLTWH(-1, -2, 4, 5);

  group('SeatLayoutTransform.fit', () {
    test('uniformly scales and centres bounds into a wide canvas', () {
      // 800×500 canvas: height is the binding axis (500/5 = 100 < 800/4 = 200).
      final t = SeatLayoutTransform.fit(bounds, const Size(800, 500));

      expect(t.scale, 100);
      // bounds top-left maps to the centred padding origin: padX = (800-400)/2 = 200.
      final topLeft = t.layoutToCanvas(bounds.left, bounds.top);
      expect(topLeft.dx, closeTo(200, 1e-9));
      expect(topLeft.dy, closeTo(0, 1e-9));
      // bounds bottom-right maps to the far centred corner.
      final bottomRight = t.layoutToCanvas(bounds.right, bounds.bottom);
      expect(bottomRight.dx, closeTo(600, 1e-9));
      expect(bottomRight.dy, closeTo(500, 1e-9));
    });

    test('uniformly scales and centres bounds into a tall canvas', () {
      // 400×600 canvas: width is the binding axis (400/4 = 100 < 600/5 = 120).
      final t = SeatLayoutTransform.fit(bounds, const Size(400, 600));

      expect(t.scale, 100);
      final topLeft = t.layoutToCanvas(bounds.left, bounds.top);
      // padX = (400-400)/2 = 0, padY = (600-500)/2 = 50.
      expect(topLeft.dx, closeTo(0, 1e-9));
      expect(topLeft.dy, closeTo(50, 1e-9));
    });

    test(
      'preserves aspect ratio: scaled width/height keep the bounds ratio',
      () {
        final t = SeatLayoutTransform.fit(bounds, const Size(333, 777));

        final tl = t.layoutToCanvas(bounds.left, bounds.top);
        final br = t.layoutToCanvas(bounds.right, bounds.bottom);
        final scaledW = br.dx - tl.dx;
        final scaledH = br.dy - tl.dy;
        expect(scaledW / scaledH, closeTo(bounds.width / bounds.height, 1e-9));
      },
    );

    test('a bigger canvas yields a bigger scale (use the extra space)', () {
      final small = SeatLayoutTransform.fit(bounds, const Size(400, 500));
      final big = SeatLayoutTransform.fit(bounds, const Size(800, 1000));

      expect(big.scale, greaterThan(small.scale));
    });
  });

  group('round-trip inverse', () {
    test('canvasToLayout(layoutToCanvas(p)) ≈ p across canvas sizes', () {
      const canvases = [Size(400, 600), Size(800, 500), Size(333, 777)];
      const points = [
        Offset(-1, -2),
        Offset(0, 0),
        Offset(1.5, 2.25),
        Offset(3, 3),
      ];

      for (final canvas in canvases) {
        final t = SeatLayoutTransform.fit(bounds, canvas);
        for (final p in points) {
          final back = t.canvasToLayout(t.layoutToCanvas(p.dx, p.dy));
          expect(back.dx, closeTo(p.dx, 1e-9));
          expect(back.dy, closeTo(p.dy, 1e-9));
        }
      }
    });
  });

  group('value equality', () {
    test('same scale + offset are equal; different are not', () {
      final a = SeatLayoutTransform.fit(bounds, const Size(400, 600));
      final b = SeatLayoutTransform.fit(bounds, const Size(400, 600));
      final c = SeatLayoutTransform.fit(bounds, const Size(800, 600));

      expect(a, equals(b));
      expect(a.hashCode, equals(b.hashCode));
      expect(a, isNot(equals(c)));
    });
  });
}
