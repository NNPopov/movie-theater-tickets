import 'package:flutter/material.dart';

import '../../../../core/common/widgets/overlay_dialog.dart';
import '../cubit/connectivity_bloc.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

class ConnectivityWidget extends StatefulWidget {
  const ConnectivityWidget({super.key});

  @override
  State<ConnectivityWidget> createState() => _ConnectivityWidget();
}

class _ConnectivityWidget extends State<ConnectivityWidget> {
  @override
  void initState() {
    super.initState();
  }

  @override
  void dispose() {
    super.dispose();
  }

  OverlayEntry? _overlayEntry;

  OverlayEntry? _disconnectedOverlayEntry;

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<ConnectivityBloc, ConnectivityState>(
        buildWhen: (context, state) {
      return false;
    }, builder: (BuildContext context, ConnectivityState state) {
      return const Text("");
    }, listener: (context, state) {
      if (state is ReconnectingState) {
        _overlayEntry = OverlayEntry(
          builder: (context) {
            return  OverlayDialog(
              header: Text(AppLocalizations.of(context)!.reconnecting_notification_text,
                  style: TextStyle(
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
        Overlay.of(context, debugRequiredFor: widget).insert(_overlayEntry!);
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
                  header:  Text(AppLocalizations.of(context)!.connection_lost_notification_text,
                      style: const TextStyle(
                        fontSize: 16,
                        color: Colors.black87,
                        decoration: TextDecoration.none,
                      )),
                  body: Container(
                    width: 120,
                    height: 50,
                    child: TextButton(
                      style: ButtonStyle(
                        padding: MaterialStateProperty.all(
                            const EdgeInsets.symmetric(vertical: 1, horizontal: 1)),
                        foregroundColor:
                            MaterialStateProperty.all<Color>(Colors.blue),
                      ),
                      onPressed: () {
                        connectivityBloc.connect();
                        _disconnectedOverlayEntry?.remove();
                        _disconnectedOverlayEntry = null;
                      },
                      child: Text(AppLocalizations.of(context)!.connection_lost_reconnect_btn),
                    ),
                  ));
            });

        Overlay.of(context, debugRequiredFor: widget)
            .insert(_disconnectedOverlayEntry!);
      }
    });
  }
}
