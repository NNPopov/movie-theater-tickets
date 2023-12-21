import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/common/widgets/overlay_dialog.dart';
import '../cubit/connectivity_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

class ConnectivitySafeAreaWidget extends StatelessWidget {
  ConnectivitySafeAreaWidget({super.key, required this.child});

  final Widget child;

  late final OverlayEntry? _overlayEntry;

  late  final OverlayEntry? _disconnectedOverlayEntry;

  @override
  Widget build(BuildContext context) {
    return BlocListener<ConnectivityBloc, ConnectivityState>(
      child: child,
      listener: (context, state) {
        if (state is ReconnectingState) {
          _overlayEntry = OverlayEntry(
            builder: (context) {
              return OverlayDialog(
                header: Text(
                    AppLocalizations.of(context)!
                        .reconnecting_notification_text,
                    style: const TextStyle(
                      fontSize: 16,
                      color: Colors.black87,
                      decoration: TextDecoration.none,
                    )),
                body: const SizedBox(
                  width: 50,
                  height: 50,
                  child: CircularProgressIndicator(),
                ),
              );
            },
          );
          Overlay.of(
            context,
          ).insert(_overlayEntry!);
        } else {
          _overlayEntry?.remove();
          _overlayEntry = null;
        }

        var connectivityBloc = context.read<ConnectivityBloc>();

        if (state is DisconnectedState) {
          _disconnectedOverlayEntry?.remove();
          _disconnectedOverlayEntry = null;

          _disconnectedOverlayEntry = OverlayEntry(
              maintainState: true,
              builder: (context) {
                return OverlayDialog(
                    header: Text(
                        AppLocalizations.of(context)!
                            .connection_lost_notification_text,
                        style: const TextStyle(
                          fontSize: 16,
                          color: Colors.black87,
                          decoration: TextDecoration.none,
                        )),
                    body: SizedBox(
                      width: 120,
                      height: 50,
                      child: TextButton(
                        onPressed: () {
                          connectivityBloc.connect();
                          _disconnectedOverlayEntry?.remove();
                          _disconnectedOverlayEntry = null;
                        },
                        child: Text(AppLocalizations.of(context)!
                            .connection_lost_reconnect_btn),
                      ),
                    ));
              });

          Overlay.of(
            context,
          ).insert(_disconnectedOverlayEntry!);
        }
      },
    );
  }
}
