// Unit tests for the pure overlay-mode resolver of slice
// 0004_connectivity_overlay_freeze_fix.
//
// `resolveOverlayMode` is the slice's deep, Flutter-free seam: a pure function from
// (connectivity state, wasConnected latch) to (overlay mode, new latch). It carries the
// "lost only after the first connect" cold-start gate. These tests pin the full transition
// table from plan.md §3 without pumping a widget.

import 'package:flutter_test/flutter_test.dart';

import 'package:movie_theater_tickets/src/hub/presentation/cubit/connectivity_bloc.dart';
import 'package:movie_theater_tickets/src/hub/presentation/widgens/connectivity_safe_area_widget.dart';

void main() {
  group('resolveOverlayMode', () {
    test(
      'seed Disconnected, never connected -> (none, false) [cold-start gate]',
      () {
        final result = resolveOverlayMode(
          DisconnectedState(),
          wasConnected: false,
        );

        expect(result.mode, ConnectivityOverlayMode.none);
        expect(result.wasConnected, isFalse);
      },
    );

    test('Reconnecting, never connected -> (reconnecting, false)', () {
      final result = resolveOverlayMode(
        ReconnectingState(),
        wasConnected: false,
      );

      expect(result.mode, ConnectivityOverlayMode.reconnecting);
      expect(result.wasConnected, isFalse);
    });

    test('Reconnecting, already connected -> (reconnecting, true)', () {
      final result = resolveOverlayMode(
        ReconnectingState(),
        wasConnected: true,
      );

      expect(result.mode, ConnectivityOverlayMode.reconnecting);
      expect(result.wasConnected, isTrue);
    });

    test('Connected -> (none, true) and latches wasConnected', () {
      final result = resolveOverlayMode(ConnectedState(), wasConnected: false);

      expect(result.mode, ConnectivityOverlayMode.none);
      expect(result.wasConnected, isTrue);
    });

    test('Disconnected after a prior connect -> (lost, true)', () {
      final result = resolveOverlayMode(
        DisconnectedState(),
        wasConnected: true,
      );

      expect(result.mode, ConnectivityOverlayMode.lost);
      expect(result.wasConnected, isTrue);
    });
  });
}
