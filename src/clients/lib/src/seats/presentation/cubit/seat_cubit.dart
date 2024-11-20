import 'dart:async';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter/material.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../../../hub/app_events.dart';
import '../../../../core/buses/event_bus.dart';
import '../../domain/entities/seat.dart';
import '../../domain/usecases/get_seats_by_movie_session_id.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

part 'seat_state.dart';

GetIt getIt = GetIt.instance;

class SeatBloc extends Bloc<SeatEvent, SeatState> {
  final storage = const FlutterSecureStorage();
  final GetSeatsByMovieSessionId _getMovieSessionById;
  final EventBus _eventBus;
  late StreamSubscription _appEventSubscription;

  late int version = 0;
  late String? _movieSessionId = '';

  SeatBloc(this._getMovieSessionById, this._eventBus)
      : super(SeatState.initState()) {
    on<SeatEvent>(_onGetSeatEvent);

    _appEventSubscription = _eventBus.stream.listen((event) {
      if (event is SeatsUpdateEvent) {
        var selectingSeat = event;

        emit(state.copyWith(
            seats: selectingSeat.seats, status: SeatStateStatus.loaded));
      }

      if (event is ShoppingCartHashIdUpdated) {
        if (_movieSessionId != null) {
          _getSeatsByMobieSessionId(_movieSessionId!);
        }
      }
    });
  }

  Future<FutureOr<void>> _onGetSeatEvent(
      SeatEvent event, Emitter<SeatState> emit) async {
    _movieSessionId = event.movieSessionId;
    _getSeatsByMobieSessionId(event.movieSessionId);
  }

  Future<void> _getSeatsByMobieSessionId(String movieSessionId) async {
    emit(state.copyWith(status: SeatStateStatus.fetching));

    final result = await _getMovieSessionById(movieSessionId);

    result.fold(
        (failure) => emit(state.copyWith(
            status: SeatStateStatus.error, errorMessage: failure.errorMessage)),
        (_) => ());
  }

  @override
  Future<void> close() async {
    await _appEventSubscription.cancel();
    return await super.close();
  }
}

@immutable
class SeatEvent extends Equatable {
  const SeatEvent({required this.movieSessionId});

  final String movieSessionId;

  @override
  List<Object> get props => [movieSessionId];
}
