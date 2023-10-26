import 'dart:async';

import '../../src/auth/domain/abstruction/auth_event_bus.dart';



class AuthEventBusImpl extends AuthEventBus {

  final StreamController<AuthEvent> _controller = StreamController.broadcast();

  Stream<AuthEvent> get stream => _controller.stream;

  @override
  void send(AuthEvent event) {
    _controller.sink.add(event);
  }

  @override
  void dispose() {
    _controller.close();
  }
}


