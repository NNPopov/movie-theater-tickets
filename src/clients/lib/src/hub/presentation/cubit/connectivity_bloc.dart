import 'dart:async';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:get_it/get_it.dart';

import '../../domain/event_hub.dart';

GetIt getIt = GetIt.instance;

class ConnectivityBloc extends Cubit<ConnectivityState> {
  ConnectivityBloc(this._eventHub):
  super(DisconnectedState()) {
    _streamSubscription = _eventHub.status.listen((event) {

      if (event is ReconnectingEvent) {
        emit(ReconnectingState());
        print("emit ReconnectingState");

      }

      if (event is DisconnectedEvent) {
        emit(DisconnectedState());
        print("emit DisconnectedState");

      }

      if (event is ConnectedEvent) {
        emit(ConnectedState());

        print("emit ConnectedState");
      }
    });
  }

  late final StreamSubscription _streamSubscription;
  late final EventHub _eventHub;

  @override
  Future<void> close() async {
    await _streamSubscription.cancel();
    return await super.close();
  }

  @override
  Future<void> connect() async {
   await _eventHub.subscribe();
  }
}

class ConnectivityEvent extends Equatable {
  @override
  List<Object?> get props => [];
}

class DisconnectedEvent extends ConnectivityEvent {
  @override
  List<Object?> get props => [];
}

class ReconnectingEvent extends ConnectivityEvent {
  @override
  List<Object?> get props => [];
}

class ConnectedEvent extends ConnectivityEvent {
  @override
  List<Object?> get props => [];
}

class ConnectivityState extends Equatable {
  @override
  List<Object?> get props => [];
}

class DisconnectedState extends ConnectivityState {
  @override
  List<Object?> get props => [];
}

class ReconnectingState extends ConnectivityState {
  @override
  List<Object?> get props => [];
}

class ConnectedState extends ConnectivityState {
  @override
  List<Object?> get props => [];
}
