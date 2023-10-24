import 'dart:async';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';

import '../event_bus.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class ConnectivityBloc extends Cubit<ConnectivityState> {
  ConnectivityBloc({EventBus? eventBus})
      : _eventBus = eventBus ?? getIt.get<EventBus>(),
        super(DisconnectedState()) {
    _eventBus.stream.listen((event) {
      if (event is DisconnectedEvent) {
        emit(DisconnectedState());
      }

      if (event is ConnectedEvent) {
        emit(ConnectedState());
      }
    });
  }

  late final EventBus _eventBus;
}

class ConnectivityEvent extends Equatable {
  @override
  List<Object?> get props => [];
}

class DisconnectedEvent extends ConnectivityEvent {
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

class ConnectedState extends ConnectivityState {
  @override
  List<Object?> get props => [];
}
