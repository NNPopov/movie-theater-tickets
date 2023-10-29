import 'dart:async';

class EventBus {
  final _controller = StreamController.broadcast();

  Stream get stream => _controller.stream;

  void send(event) {
    _controller.sink.add(event);
  }

  @override
  void dispose() {
    _controller.close();

  }
}

