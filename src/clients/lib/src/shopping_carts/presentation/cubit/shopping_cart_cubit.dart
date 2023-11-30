import 'dart:async';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter/cupertino.dart';
import 'package:get_it/get_it.dart';
import 'package:movie_theater_tickets/core/errors/failures.dart';
import '../../../helpers/constants.dart';
import '../../../../core/buses/event_bus.dart';
import '../../../shopping_carts/domain/usecases/create_shopping_cart.dart';
import '../../domain/entities/seat.dart';
import '../../domain/entities/shopping_cart.dart';
import '../../domain/usecases/get_shopping_cart.dart';
import '../../domain/usecases/reserve_seats.dart';
import '../../domain/usecases/select_seat.dart';
import '../../domain/usecases/shopping_cart_subscribe.dart';
import '../../domain/usecases/unselect_seat.dart';
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
      : super(ShoppingCartState.initState()) {
    getShoppingCartIfExits();

    _streamSubscription = _eventBus.stream.listen((event) async {
      if (event is ShoppingCartDeleteEvent) {
        emit(ShoppingCartState.deletedState());
        emit(ShoppingCartState.initState());
      }

      if (event is ShoppingCartUpdateEvent) {
        emit(state.copyWith(shoppingCart: event.shoppingCart, status: ShoppingCartStateStatus.update));
      }

      if (event is ShoppingCartHashIdIdUpdateEvent) {
        var key = await storage.read(key: Constants.SHOPPING_CARD_HASH_ID);
        if (key != null) {
          emit(state.copyWith(hashId:key));
        }
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
  late final ShoppingCartUpdateSubscribeUseCase
      _shoppingCartUpdateSubscribeUseCase;
  late final ReserveSeatsUseCase _reserveSeatsUseCase;

  String hashId = '';


  Future<void> updateShoppingCartState(
      ShoppingCartUpdateEvent event, Emitter<ShoppingCartState> emit) async {

    emit(state.copyWith(shoppingCart: event.shoppingCart, status:ShoppingCartStateStatus.update));
  }

  Future<void> createShoppingCart(int maxNumberOfSeats) async {

    emit(state.copyWith(status:ShoppingCartStateStatus.creating));

    final result = await _createShoppingCart(
        CreateShoppingCartCommand(maxNumberOfSeats: maxNumberOfSeats));


    result.fold((failure) {
      if (failure is ValidationFailure) {
        emit(state.copyWith(status: ShoppingCartStateStatus.createValidationError, errorMessage: failure.message));
        return;
      }
      emit(state.copyWith(status: ShoppingCartStateStatus.error, errorMessage: failure.message));

      return;
    }, (value) async {
      final resultShoppingCart = await _getShoppingCart();

      resultShoppingCart.fold((failure) {
        if (failure.statusCode == 204) {
          emit(ShoppingCartState.initState());
        } else {
          emit(state.copyWith(status: ShoppingCartStateStatus.error, errorMessage: failure.message));
        }
      }, (shoppingCartValue) async {
        hashId = value;

        emit(state.copyWith(shoppingCart: shoppingCartValue, status:ShoppingCartStateStatus.created, hashId:hashId));
        emit(state.copyWith( status:ShoppingCartStateStatus.update));

      });
    });
  }

  Future<void> getShoppingCart() async {

    emit(state.copyWith(status:ShoppingCartStateStatus.creating));

    final result = await _getShoppingCart();

    result.fold((failure) {
      if (failure.statusCode == 204) {
        emit(ShoppingCartState.initState());
      } else {
        emit(ShoppingCartState
            .initState()
            .copyWith(status: ShoppingCartStateStatus.error, errorMessage: failure.message));
      }
    }, (value) async {
      var key = await storage.read(key: Constants.SHOPPING_CARD_HASH_ID);
      if (key != null) {
        hashId = key;
      }
      emit(state.copyWith(shoppingCart: value, status:ShoppingCartStateStatus.created, hashId:hashId));

    });
  }

  Future<void> seatSelect(
      {required int row,
      required int seatNumber,
      required String movieSessionId}) async {
    final shoppingCartSeat =
        ShoppingCartSeat(seatRow: row, seatNumber: seatNumber);

    var command = SelectSeatCommand(seat: shoppingCartSeat, movieSessionId: movieSessionId);

    var result = await _selectSeatUseCase(command);

    result.fold((l) {

      emit(state.copyWith(status: ShoppingCartStateStatus.error, errorMessage: l.errorMessage));
      emit(state.copyWith(status:ShoppingCartStateStatus.update));
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
        (l) {

          emit(state.copyWith(status: ShoppingCartStateStatus.error, errorMessage: l.errorMessage));
          emit(state.copyWith(status:ShoppingCartStateStatus.update));

        },
        (r) async {});
  }

  Future<void> completePurchase() async {
    var result = await _reserveSeatsUseCase();

    result.fold((l) {
      emit(state.copyWith(status: ShoppingCartStateStatus.error, errorMessage: l.errorMessage));
      emit(state.copyWith(status:ShoppingCartStateStatus.update));
    }, (r) async {});
  }

  @override
  Future<void> close() async {
    await _streamSubscription.cancel();
    return await super.close();
  }
}
