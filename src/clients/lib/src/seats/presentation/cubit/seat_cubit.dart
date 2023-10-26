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
  late List<Seat> _seats;
 // late final StreamSubscription<ShoppingCartState> _shoppingCartStream;
  final storage = const FlutterSecureStorage();
  late GetSeatsByMovieSessionId _getMovieSessionById;

 // late HubConnection _hubConnection;
  late String _hashId;
  late int version = 0;


  late EventBus _eventBus;

  late StreamSubscription _appEventSubscription;

  SeatCubit(
      {GetSeatsByMovieSessionId? getMovieSessionById, EventBus? eventBus})
      : _getMovieSessionById =
            getMovieSessionById ?? getIt.get<GetSeatsByMovieSessionId>(),
        _eventBus =
            eventBus ?? getIt.get<EventBus>(),
        super(const InitialState()) {


    _hashId = "";


    _appEventSubscription = _eventBus.stream.listen((event) {
      if (event is SeatsUpdateEvent ) {

        var selectingSeat = event as SeatsUpdateEvent;

        emit(SeatsState(selectingSeat.seats));
      }
    });

    // _shoppingCartStream = shoppingCartCubit.stream.listen((event) {
    //   if (event is ShoppingCartCreatedState ||
    //       event is ShoppingCartCreatedState) {
    //     var selectingSeat = event as ShoppingCartCreatedState;
    //
    //     _hashId = selectingSeat.hashId;
    //     updateSeatsState(selectingSeat.shoppingCard);
    //   }
    // });
  }

  get movies => null;

  void updateSeatsState(ShoppingCart shoppingCard) {}


  bool checkIsCurrentReserve(Seat e) {


    if (_hashId.isEmpty) {
      return false;
    }
    if (e.hashId == _hashId) {
      return true;
    }
    return false;
  }

  Future<void> getSeats(String movieSessionId) async {
    emit(const GettingSeats());
    // if (_hubConnection.state != HubConnectionState.Connected) {
    //   await _hubConnection.start();
    //   //connectionIsOpen = true;
    // }
  //  _hubConnection.invoke("JoinGroup", args: <Object>[movieSessionId]);

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
