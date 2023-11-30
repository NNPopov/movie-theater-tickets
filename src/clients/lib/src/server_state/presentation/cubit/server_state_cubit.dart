import 'dart:async';
import 'package:bloc/bloc.dart';
import '../../../../core/buses/event_bus.dart';
import '../../domain/entities/server_state.dart';

class ServerStateCubit extends Cubit<ServerState> {
  ServerStateCubit(
      this._eventBus)
      : super(ServerState.initState()) {

    _streamSubscription = _eventBus.stream.listen((event) async {
      if (event is ServerState) {
        emit(event);
      }

    });
  }

  late final StreamSubscription _streamSubscription;
  late final EventBus _eventBus;

  @override
  Future<void> close() async {
    await _streamSubscription.cancel();
    return await super.close();
  }
}
