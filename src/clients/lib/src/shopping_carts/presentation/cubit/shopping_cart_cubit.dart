import 'dart:async';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:get_it/get_it.dart';
import 'package:movie_theater_tickets/core/errors/failures.dart';
import '../../../helpers/constants.dart';
import '../../../../core/buses/event_bus.dart';
import '../../../shopping_carts/domain/usecases/create_shopping_cart.dart';
import '../../domain/entities/seat.dart';
import '../../domain/entities/shopping_cart.dart';
import '../../domain/services/shopping_cart_service.dart';
import '../../domain/usecases/assign_client_use_case.dart';
import '../../domain/usecases/get_shopping_cart.dart';
import '../../domain/usecases/reserve_seats.dart';
import '../../domain/usecases/select_seat.dart';
import '../../domain/usecases/shopping_cart_subscribe.dart';
import '../../domain/usecases/unselect_seat.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

part 'shopping_cart_state.dart';

part 'shopping_cart_event.dart';

GetIt getIt = GetIt.instance;

class ShoppingCartCubit extends Cubit<ShoppingCartState> {
  ShoppingCartCubit(
      this._createShoppingCart,
      this._selectSeatUseCase,
      this._unselectSeatUseCase,
      this._getShoppingCart,
      this._shoppingCartUpdateSubscribeUseCase,
      this._reserveSeatsUseCase,
      this._eventBus)
      : super(ShoppingCartInitialState(ShoppingCart.empty(), 1, '')) {
    getShoppingCartIfExits();

    _streamSubscription = _eventBus.stream.listen((event) {
      if (event is ShoppingCartDeleteEvent) {
        version = version + 1;
        emit(ShoppingCartDeleteState(ShoppingCart.empty(), version, hashId));
        emit(ShoppingCartInitialState(ShoppingCart.empty(), version, hashId));
      }

      if (event is ShoppingCartUpdateEvent) {
        version = version + 1;
        emit(ShoppingCartCurrentState(event.shoppingCart, version, hashId));
      }
    });
  }

  Future<void> getShoppingCartIfExits() async => await getShoppingCart();

  late final StreamSubscription _streamSubscription;
  final storage = const FlutterSecureStorage();

  late final EventBus _eventBus;
  late final CreateShoppingCartUseCase _createShoppingCart;
  late final SelectSeatUseCase _selectSeatUseCase;
  late final UnselectSeatUseCase _unselectSeatUseCase;
  late final GetShoppingCartUseCase _getShoppingCart;
  late final ShoppingCartUpdateSubscribeUseCase      _shoppingCartUpdateSubscribeUseCase;
  late final ReserveSeatsUseCase _reserveSeatsUseCase;


  String hashId = '';
  late int version = 0;

  Future<void> updateShoppingCartState(
      ShoppingCartUpdateEvent event, Emitter<ShoppingCartState> emit) async {
    version = version + 1;
    emit(ShoppingCartCurrentState(event.shoppingCart, version, hashId));
  }

  Future<void> createShoppingCart(int maxNumberOfSeats) async {
    version = version + 1;
    emit(CreatingShoppingCart(ShoppingCart.empty(), version, hashId));

    final result = await _createShoppingCart(
        CreateShoppingCartCommand(maxNumberOfSeats: maxNumberOfSeats));

    version = version + 1;
    result.fold((failure) {
      if (failure is ValidationFailure) {
        emit(ShoppingCartCreateValidationErrorState(
            ShoppingCart.empty(), version, hashId, failure.message));
        return;
      }

      emit(ShoppingCartError(
          ShoppingCart.empty(), version, hashId, failure.errorMessage));
      return;
    }, (value) async {
      final resultShoppingCart = await _getShoppingCart();

      resultShoppingCart.fold((failure) {
        if (failure.statusCode == 204) {
          emit(ShoppingCartInitialState(ShoppingCart.empty(), version, hashId));
        } else {
          emit(ShoppingCartError(
              ShoppingCart.empty(), version, hashId, failure.errorMessage));
        }
      }, (shoppingCartValue) async {
        hashId = value;
        emit(ShoppingCartCreatedState(shoppingCartValue, version, hashId));
        emit(ShoppingCartCurrentState(shoppingCartValue, version, hashId));
      });
    });
  }

  Future<void> getShoppingCart() async {
    version = version + 1;
    emit(CreatingShoppingCart(ShoppingCart.empty(), version, hashId));

    final result = await _getShoppingCart();

    result.fold((failure) {
      if (failure.statusCode == 204) {
        emit(ShoppingCartInitialState(ShoppingCart.empty(), version, hashId));
      } else {
        emit(ShoppingCartError(
            ShoppingCart.empty(), version, hashId, failure.errorMessage));
      }
    }, (value) async {
      var key = await storage.read(key: Constants.SHOPPING_CARD_HASH_ID);
      if (key != null) {
        hashId = key!;
      }
      emit(ShoppingCartCurrentState(value, version, hashId));
    });
  }

  Future<void> seatSelect(
      {required int row,
      required int seatNumber,
      required String movieSessionId}) async {
    final shoppingCartSeat =
        ShoppingCartSeat(seatRow: row, seatNumber: seatNumber);

    var command = SelectSeatCommand(
        seat: shoppingCartSeat, movieSessionId: movieSessionId);

    var result = await _selectSeatUseCase(command);

    result.fold((l) {
      version = version + 1;
      emit(ShoppingCartError(
          state.shoppingCard, version, hashId, l.errorMessage));
    }, (r) async {});
  }

  Future<void> unSeatSelect(
      {required int row,
      required int seatNumber,
      required String movieSessionId}) async {
    final shoppingCartSeat =
        ShoppingCartSeat(seatRow: row, seatNumber: seatNumber);

    var command = SelectSeatCommand(
        seat: shoppingCartSeat, movieSessionId: movieSessionId);

    var result = await _unselectSeatUseCase(command);
    result.fold(
        (l) => emit(ShoppingCartError(
            state.shoppingCard, version, hashId, l.errorMessage)),
        (r) async {});
  }

  Future<void> completePurchase() async {
    var result = await _reserveSeatsUseCase();

    result.fold((l) {
      version = version + 1;
      emit(ShoppingCartError(
          state.shoppingCard, version, hashId, l.errorMessage));
    }, (r) async {});
  }

  @override
  Future<void> close() async {
    await _streamSubscription.cancel();
    return await super.close();
  }
}
