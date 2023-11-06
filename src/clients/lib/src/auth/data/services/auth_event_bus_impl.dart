import 'dart:async';

import '../../domain/abstraction/auth_event_bus.dart';



class AuthEventBusImpl implements AuthEventBus {

  final StreamController<AuthStatus> _controller = StreamController.broadcast();

  Stream<AuthStatus> get stream => _controller.stream;

  @override
  void send(AuthStatus status) {
    _controller.sink.add(status);
  }

  @override
  void dispose() {
    _controller.close();
  }
}


