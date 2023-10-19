import 'dart:async';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../../../helpers/constants.dart';
import '../../../shopping_carts/domain/usecases/create_shopping_cart.dart';
import '../../../shopping_carts/presentation/cubit/shopping_cart_cubit.dart';
import '../../domain/entities/seat.dart';
import '../../domain/usecases/get_seats_by_movie_session_id.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

part 'seat_state.dart';

GetIt getIt = GetIt.instance;

class SeatCubit extends Cubit<SeatState> {
  SeatCubit(
      {GetSeatsByMovieSessionId? getMovieSessionById,
      required ShoppingCartCubit shoppingCartCubit})
      : _getMovieSessionById =
            getMovieSessionById ?? getIt.get<GetSeatsByMovieSessionId>(),
        // _shoppingCartCubit = shoppingCartCubit,
        super(const InitialState()) {
    _shoppingCartStream = shoppingCartCubit.stream.listen((event) {
      print(event);
      if (event is ShoppingCartCurrentState) {
        var selectingSeat = event as ShoppingCartCurrentState;

        if (selectingSeat.shoppingCard.shoppingCartSeat != null &&
            selectingSeat.shoppingCard.shoppingCartSeat.length >= 0) {
          List<Seat> newSeat = _seats.map((e) {
            var currentSeats = selectingSeat.shoppingCard.shoppingCartSeat
                .any((t) => e.row == t.seatRow && e.seatNumber == t.seatNumber);

            return Seat(
                row: e.row, seatNumber: e.seatNumber, blocked: currentSeats);
          }).toList();

          _seats = newSeat;
        }
        emit(SeatsState(_seats));
      }
    });
  }

  late List<Seat> _seats;

  //late final ShoppingCartCubit _shoppingCartCubit;
  late final StreamSubscription<ShoppingCartState> _shoppingCartStream;

  final storage = const FlutterSecureStorage();

  late GetSeatsByMovieSessionId _getMovieSessionById;

  Future<void> getSeats(String movieSessionId) async {
    emit(const GettingSeats());

    final result = await _getMovieSessionById(movieSessionId);
    result.fold((failure) => emit(SeatsError(failure.errorMessage)),
        (seats) => {_seats = seats, emit(SeatsState(seats))});
  }

  @override
  Future<void> close() {
    _shoppingCartStream.cancel();
    return super.close();
  }
}
