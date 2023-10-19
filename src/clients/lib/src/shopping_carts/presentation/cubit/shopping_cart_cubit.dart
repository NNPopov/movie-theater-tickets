import 'dart:async';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:get_it/get_it.dart';
import '../../../shopping_carts/domain/usecases/create_shopping_cart.dart';
import '../../domain/entities/seat.dart';
import '../../domain/entities/shopping_cart.dart';
import '../../domain/usecases/get_shopping_cart.dart';
import '../../domain/usecases/select_seat.dart';
import '../../domain/usecases/unselect_seat.dart';

part 'shopping_cart_state.dart';

GetIt getIt = GetIt.instance;

class ShoppingCartCubit extends Cubit<ShoppingCartState> {
  ShoppingCartCubit(
      {CreateShoppingCart? createShoppingCart,
      SelectSeatUseCase? selectSeatUseCase,
      UnselectSeatUseCase? unselectSeatUseCase,
      GetShoppingCart? getShoppingCart})
      : _createShoppingCart =
            createShoppingCart ?? getIt.get<CreateShoppingCart>(),
        _selectSeatUseCase =
            selectSeatUseCase ?? getIt.get<SelectSeatUseCase>(),
        _unselectSeatUseCase =
            unselectSeatUseCase ?? getIt.get<UnselectSeatUseCase>(),
        _getShoppingCart = getShoppingCart ?? getIt.get<GetShoppingCart>(),
        super(const ShoppingCartInitialState());

  late CreateShoppingCart _createShoppingCart;
  late SelectSeatUseCase _selectSeatUseCase;
  late UnselectSeatUseCase _unselectSeatUseCase;
  late GetShoppingCart _getShoppingCart;

  late ShoppingCart _shoppingCart;

  late int version = 0;

  Future<void> createShoppingCart(int maxNumberOfSeats) async {
    emit(const CreatingShoppingCart());

    var createShoppingCartCommand =
        CreateShoppingCartCommand(maxNumberOfSeats: maxNumberOfSeats);

    final result = await _createShoppingCart(createShoppingCartCommand);

    result.fold((failure) => emit(ShoppingCartError(failure.errorMessage)),
        (seats) async {
      final resultShoppingCart = await _getShoppingCart(seats);

      resultShoppingCart
          .fold((failure) => emit(ShoppingCartError(failure.errorMessage)),
              (shoppingCartValue) {
        _shoppingCart = shoppingCartValue;

        version = version + 1;
        emit(ShoppingCartCurrentState(shoppingCartValue, version));
      });
    });
  }

  Future<void> getShoppingCart(String shoppingCartId) async {
    emit(const CreatingShoppingCart());

    final result = await _getShoppingCart(shoppingCartId);

    result.fold((failure) => emit(ShoppingCartError(failure.errorMessage)),
        (value) {
      version = version + 1;
      emit(ShoppingCartCurrentState(value, version));
    });
  }

  Future<void> seatSelect(
      {required int row,
      required int seatNumber,
      required String movieSessionId}) async {
    final shoppingCartSeat =
        ShoppingCartSeat(seatRow: row, seatNumber: seatNumber);

    var result = _shoppingCart.addSeat(shoppingCartSeat);
    version = version + 1;

    result.fold((l) => emit(ShoppingCartError(l.message)), (r) async {
      var command = SelectSeatCommand(
          seat: shoppingCartSeat,
          shoppingCart: _shoppingCart,
          movieSessionId: movieSessionId);

      await _selectSeatUseCase(command);

      emit(ShoppingCartCurrentState(_shoppingCart, version));
    });
  }

  Future<void> unSeatSelect(
      {required int row,
      required int seatNumber,
      required String movieSessionId}) async {
    final shoppingCartSeat =
        ShoppingCartSeat(seatRow: row, seatNumber: seatNumber);

    _shoppingCart.deleteSeat(shoppingCartSeat);
    version = version + 1;

    emit(ShoppingCartCurrentState(_shoppingCart, version));

    var command = SelectSeatCommand(
        seat: shoppingCartSeat,
        shoppingCart: _shoppingCart,
        movieSessionId: movieSessionId);

    await _unselectSeatUseCase(command);
  }
}
