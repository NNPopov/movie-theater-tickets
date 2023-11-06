import 'dart:async';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:get_it/get_it.dart';
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
      {ShoppingCartService? shoppingCartService,
      CreateShoppingCartUseCase? createShoppingCartUseCase,
      SelectSeatUseCase? selectSeatUseCase,
      UnselectSeatUseCase? unselectSeatUseCase,
      GetShoppingCart? getShoppingCartUseCase,
      ShoppingCartUpdateSubscribeUseCase? shoppingCartUpdateSubscribeUseCase,
      AssignClientUseCase? assignClientUseCase,
      ReserveSeatsUseCase? reserveSeatsUseCase,
      EventBus? eventBus})
      : _shoppingCartService =
            shoppingCartService ?? getIt<ShoppingCartService>(),
        _createShoppingCart =
            createShoppingCartUseCase ?? getIt.get<CreateShoppingCartUseCase>(),
        _selectSeatUseCase =
            selectSeatUseCase ?? getIt.get<SelectSeatUseCase>(),
        _unselectSeatUseCase =
            unselectSeatUseCase ?? getIt.get<UnselectSeatUseCase>(),
        _getShoppingCart =
            getShoppingCartUseCase ?? getIt.get<GetShoppingCart>(),
        _shoppingCartUpdateSubscribeUseCase =
            shoppingCartUpdateSubscribeUseCase ??
                getIt.get<ShoppingCartUpdateSubscribeUseCase>(),
        _assignClientUseCase =
            assignClientUseCase ?? getIt.get<AssignClientUseCase>(),
        _reserveSeatsUseCase =
            reserveSeatsUseCase ?? getIt.get<ReserveSeatsUseCase>(),
        _hashId = "",
        _eventBus = eventBus ?? getIt.get<EventBus>(),
        super(ShoppingCartInitialState(ShoppingCart.empty(), '', 1)) {
    GetShoppingCartIfExits();

    _streamSubscription = _eventBus.stream.listen((event) {
      if (event is ShoppingCartUpdateEvent) {
        version = version + 1;
        emit(ShoppingCartCurrentState(event.shoppingCart, _hashId, version));
      }
    });
  }

  Future<void> GetShoppingCartIfExits() async {
    var shoppingCartId = await storage.read(key: Constants.SHOPPING_CARD_ID);
    var hashId = await storage.read(key: Constants.SHOPPING_CARD_HASH_ID);

    if (shoppingCartId != null && hashId != null) {
      getShoppingCart(shoppingCartId);
    }
  }

  late final StreamSubscription _streamSubscription;
  final storage = const FlutterSecureStorage();

  late final ShoppingCartService _shoppingCartService;
  late final EventBus _eventBus;
  late final CreateShoppingCartUseCase _createShoppingCart;
  late final SelectSeatUseCase _selectSeatUseCase;
  late final UnselectSeatUseCase _unselectSeatUseCase;
  late final GetShoppingCart _getShoppingCart;
  late final ShoppingCartUpdateSubscribeUseCase
      _shoppingCartUpdateSubscribeUseCase;
  late final ReserveSeatsUseCase _reserveSeatsUseCase;

  late final AssignClientUseCase _assignClientUseCase;

   late String _hashId;
  late int version = 0;

  Future<void> updateShoppingCartState(
      ShoppingCartUpdateEvent event, Emitter<ShoppingCartState> emit) async {
    version = version + 1;
    emit(ShoppingCartCurrentState(event.shoppingCart, _hashId, version));
  }

  Future<void> createShoppingCart(int maxNumberOfSeats) async {
    if (maxNumberOfSeats > 4 || maxNumberOfSeats < 1) {
      emit(ShoppingCartCreateValidationErrorState(ShoppingCart.empty(), _hashId,
          version, 'Number of places should be from 1 to 4'));
      return;
    }
    version = version + 1;
    emit(CreatingShoppingCart(ShoppingCart.empty(), _hashId, version));
    final result =
        await _shoppingCartService.createShoppingCart(maxNumberOfSeats);

    version = version + 1;

    result.fold((failure) {
      version = version + 1;
      emit(ShoppingCartError(
          ShoppingCart.empty(), _hashId, version, failure.errorMessage));

    }, (value) async {
   //   _hashId = value.hashId;

      final resultShoppingCart = await _getShoppingCart(value.shoppingCartId);

      resultShoppingCart.fold((failure) {
        if (failure.statusCode == 204) {
          emit(
              ShoppingCartInitialState(ShoppingCart.empty(), _hashId, version));
        } else {
          emit(ShoppingCartError(
              ShoppingCart.empty(), _hashId, version, failure.errorMessage));
        }
      }, (shoppingCartValue) async {
        version = version + 1;
        emit(ShoppingCartCurrentState(shoppingCartValue, _hashId, version));
      });
    });
  }

  Future<void> getShoppingCart(String shoppingCartId) async {
    version = version + 1;
    emit(CreatingShoppingCart(ShoppingCart.empty(), _hashId, version));

    final result = await _getShoppingCart(shoppingCartId);

    result.fold((failure) {
      if (failure.statusCode == 204) {
        emit(ShoppingCartInitialState(ShoppingCart.empty(), _hashId, version));
      } else {
        emit(ShoppingCartError(
            ShoppingCart.empty(), _hashId, version, failure.errorMessage));
      }
    }, (value) {
      _shoppingCartUpdateSubscribeUseCase(shoppingCartId);
      emit(ShoppingCartCurrentState(value, _hashId, version));
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

    result.fold(
        (l) => emit(ShoppingCartError(
            state.shoppingCard, _hashId, version, l.errorMessage)),
        (r) async {});
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
            state.shoppingCard, _hashId, version, l.errorMessage)),
        (r) async {});
  }

  Future<void> completePurchase() async {
    var result = await _reserveSeatsUseCase();

    result.fold(
        (l) => emit(ShoppingCartError(
            state.shoppingCard, _hashId, version, l.errorMessage)),
        (r) async {});
  }

  @override
  Future<void> close() async {
    await _streamSubscription.cancel();
    return await super.close();
  }
}
