import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/common/widgets/overlay_dialog.dart';
import '../cubit/connectivity_bloc.dart';
import 'package:movie_theater_tickets/l10n/gen/app_localizations.dart';

/// Which connectivity overlay is currently on screen. The overlay is a pure
/// function of the connectivity state plus a `wasConnected` latch — never an
/// imperatively managed [OverlayEntry] — so the overlay and the connectivity
/// state can never desynchronize.
enum ConnectivityOverlayMode { none, reconnecting, lost }

/// Result of [resolveOverlayMode]: the overlay [mode] to render and the updated
/// `wasConnected` latch.
typedef OverlayResolution = ({ConnectivityOverlayMode mode, bool wasConnected});

/// Pure, unit-testable resolver mapping a connectivity [state] (plus whether the
/// app has ever connected) to the overlay mode to display.
///
/// `wasConnected` starts `false`, flips to `true` the first time [ConnectedState]
/// is seen, and never flips back. The "connection lost" dialog is shown only once
/// the app has connected at least once (the cold-start gate).
///
/// No [BuildContext] or widget dependency — this is the slice's deep test seam.
OverlayResolution resolveOverlayMode(
  ConnectivityState state, {
  required bool wasConnected,
}) {
  if (state is ReconnectingState) {
    return (
      mode: ConnectivityOverlayMode.reconnecting,
      wasConnected: wasConnected,
    );
  }
  if (state is ConnectedState) {
    return (mode: ConnectivityOverlayMode.none, wasConnected: true);
  }
  // DisconnectedState (seed / cold start / loss after a connection).
  return wasConnected
      ? (mode: ConnectivityOverlayMode.lost, wasConnected: true)
      : (mode: ConnectivityOverlayMode.none, wasConnected: false);
}

class ConnectivitySafeAreaWidget extends StatefulWidget {
  const ConnectivitySafeAreaWidget({super.key, required this.child});

  final Widget child;

  @override
  State<ConnectivitySafeAreaWidget> createState() =>
      _ConnectivitySafeAreaWidgetState();
}

class _ConnectivitySafeAreaWidgetState
    extends State<ConnectivitySafeAreaWidget> {
  ConnectivityOverlayMode _mode = ConnectivityOverlayMode.none;
  bool _wasConnected = false;

  static const TextStyle _headerStyle = TextStyle(
    fontSize: 16,
    color: Colors.black87,
    decoration: TextDecoration.none,
  );

  @override
  Widget build(BuildContext context) {
    return BlocListener<ConnectivityBloc, ConnectivityState>(
      listener: (context, state) {
        final next = resolveOverlayMode(state, wasConnected: _wasConnected);
        if (next.mode != _mode || next.wasConnected != _wasConnected) {
          setState(() {
            _mode = next.mode;
            _wasConnected = next.wasConnected;
          });
        }
      },
      child: Stack(children: [widget.child, ..._overlayLayer(context)]),
    );
  }

  List<Widget> _overlayLayer(BuildContext context) {
    switch (_mode) {
      case ConnectivityOverlayMode.none:
        return const [];
      case ConnectivityOverlayMode.reconnecting:
        return [
          OverlayDialog(
            header: Text(
              AppLocalizations.of(context)!.reconnecting_notification_text,
              style: _headerStyle,
            ),
            body: const SizedBox(
              width: 50,
              height: 50,
              child: CircularProgressIndicator(),
            ),
          ),
        ];
      case ConnectivityOverlayMode.lost:
        return [
          OverlayDialog(
            header: Text(
              AppLocalizations.of(context)!.connection_lost_notification_text,
              style: _headerStyle,
            ),
            body: SizedBox(
              width: 120,
              height: 50,
              child: TextButton(
                onPressed: () => context.read<ConnectivityBloc>().connect(),
                child: Text(
                  AppLocalizations.of(context)!.connection_lost_reconnect_btn,
                ),
              ),
            ),
          ),
        ];
    }
  }
}
