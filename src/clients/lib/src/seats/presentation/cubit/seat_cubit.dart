import 'dart:async';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
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
  late GetSeatsByMovieSessionId _getMovieSessionById;

  late int version = 0;

  late EventBus _eventBus;

  late StreamSubscription _appEventSubscription;

  SeatCubit({GetSeatsByMovieSessionId? getMovieSessionById, EventBus? eventBus})
      : _getMovieSessionById =
            getMovieSessionById ?? getIt.get<GetSeatsByMovieSessionId>(),
        _eventBus = eventBus ?? getIt.get<EventBus>(),
        super(const InitialState()) {


    _appEventSubscription = _eventBus.stream.listen((event) {
      if (event is SeatsUpdateEvent) {
        var selectingSeat = event as SeatsUpdateEvent;

        emit(SeatsState(selectingSeat.seats));
      }
    });
  }

  get movies => null;

  void updateSeatsState(ShoppingCart shoppingCard) {}

  Future<void> getSeats(String movieSessionId) async {
    emit(const GettingSeats());

    final result = await _getMovieSessionById(movieSessionId);

    result.fold((failure) => emit(SeatsError(failure.errorMessage)),
        (seats) async {
      emit(SeatsState(seats));
    });
  }

  @override
  Future<void> close() async {
    await _appEventSubscription.cancel();
    return await super.close();
  }
}
