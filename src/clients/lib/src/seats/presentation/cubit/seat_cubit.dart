import 'dart:async';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter/material.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../../../hub/app_events.dart';
import '../../../../core/buses/event_bus.dart';
import '../../../shopping_carts/domain/entities/shopping_cart.dart';
import '../../domain/entities/seat.dart';
import '../../domain/usecases/get_seats_by_movie_session_id.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

part 'seat_state.dart';

GetIt getIt = GetIt.instance;

class SeatCubit extends Cubit<SeatState> {
  final storage = const FlutterSecureStorage();
  final GetSeatsByMovieSessionId _getMovieSessionById;
  final EventBus _eventBus;
  late StreamSubscription _appEventSubscription;

  late int version = 0;
  late String? _movieSessionId = '';

  SeatCubit(this._getMovieSessionById, this._eventBus)
      : super(SeatState.initState()) {
    _appEventSubscription = _eventBus.stream.listen((event) {
      if (event is SeatsUpdateEvent) {
        var selectingSeat = event;

        emit(state.copyWith(
            seats: selectingSeat.seats, status: SeatStateStatus.loaded));
      }

      if (event is ShoppingCartHashIdUpdated) {
        if (_movieSessionId != null) {
          _getSeats(_movieSessionId!);
        }
      }
    });
  }

  get movies => null;

  void updateSeatsState(ShoppingCart shoppingCard) {}

  Future<void> getSeats(String movieSessionId) async {
    _movieSessionId = movieSessionId;
    _getSeats(movieSessionId);
  }

  Future<void> _getSeats(String movieSessionId) async {
    _movieSessionId = movieSessionId;

    emit(state.copyWith(status: SeatStateStatus.fetching));

    final result = await _getMovieSessionById(movieSessionId);

    result.fold(
        (failure) => emit(state.copyWith(
            status: SeatStateStatus.error,
            errorMessage: failure.errorMessage)), (seats) async {
      emit(state.copyWith(seats: seats, status: SeatStateStatus.loaded));
    });
  }

  @override
  Future<void> close() async {
    await _appEventSubscription.cancel();
    return await super.close();
  }
}
