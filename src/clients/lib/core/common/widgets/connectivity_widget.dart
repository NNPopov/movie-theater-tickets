import 'package:flutter/material.dart';

import '../../../src/hub/connectivity/connectivity_bloc.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

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

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<ConnectivityBloc, ConnectivityState>(
        buildWhen: (context, state) {
      return false;
    }, builder: (BuildContext context, ConnectivityState state) {
      return const Text("");
    }, listener: (context, state) {
      if (state is DisconnectedState) {
        _overlayEntry = OverlayEntry(
          builder: (context) {
            return Container(
              color: Colors.grey.withOpacity(0.5),
              alignment: Alignment.center,
              child: const Expanded(
                  child: SizedBox(
                width: 400,
                height: 400,
                child: CircularProgressIndicator(),
              )),
            );
          },
        );

        Overlay.of(context).insert(_overlayEntry!);
      } else {
        if (_overlayEntry != null) {
          _overlayEntry!.remove();
        }
      }
    });
  }
}
