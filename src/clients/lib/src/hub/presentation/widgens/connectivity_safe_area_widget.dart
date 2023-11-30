import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/common/widgets/overlay_dialog.dart';
import '../cubit/connectivity_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

class ConnectivitySafeAreaWidget extends StatelessWidget {
   ConnectivitySafeAreaWidget(
      {super.key,
        required this.child});

  final Widget child;


 late OverlayEntry? _overlayEntry;

  late OverlayEntry? _disconnectedOverlayEntry;

  @override
  Widget build(BuildContext context) {
    return               BlocListener<ConnectivityBloc, ConnectivityState>(
      child: child ,
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
                        style: ButtonStyle(
                          padding: MaterialStateProperty.all(
                              const EdgeInsets.symmetric(
                                  vertical: 1, horizontal: 1)),
                          foregroundColor:
                          MaterialStateProperty.all<Color>(
                              Colors.blue),
                        ),
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
