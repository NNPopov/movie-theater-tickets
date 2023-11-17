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

        _eventBus = eventBus ?? getIt.get<EventBus>(),
        super(ShoppingCartInitialState(ShoppingCart.empty(), 1)) {
    GetShoppingCartIfExits();

    _streamSubscription = _eventBus.stream.listen((event) {
      if (event is ShoppingCartUpdateEvent) {
        version = version + 1;
        emit(ShoppingCartCurrentState(event.shoppingCart, version));
      }
    });
  }

  Future<void> GetShoppingCartIfExits() async {

    await  getShoppingCart();

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


  late int version = 0;

  Future<void> updateShoppingCartState(
      ShoppingCartUpdateEvent event, Emitter<ShoppingCartState> emit) async {
    version = version + 1;
    emit(ShoppingCartCurrentState(event.shoppingCart,  version));
  }

  Future<void> createShoppingCart(int maxNumberOfSeats) async {

    version = version + 1;
    emit(CreatingShoppingCart(ShoppingCart.empty(),  version));

    final result =
        await _createShoppingCart(CreateShoppingCartCommand(maxNumberOfSeats: maxNumberOfSeats));

    version = version + 1;
    result.fold((failure) {

      if(failure is ValidationFailure)
        {
          emit(ShoppingCartCreateValidationErrorState(ShoppingCart.empty(), version, failure.message));
          return;
        }

      emit(ShoppingCartError(ShoppingCart.empty(),  version, failure.errorMessage));
      return;

    }, (value) async {

      final resultShoppingCart = await _getShoppingCart();

      resultShoppingCart.fold((failure) {
        if (failure.statusCode == 204) {
          emit(
              ShoppingCartInitialState(ShoppingCart.empty(),  version));
        } else {
          emit(ShoppingCartError(
              ShoppingCart.empty(), version, failure.errorMessage));
        }
      }, (shoppingCartValue) async {
        emit(ShoppingCartCreatedState(shoppingCartValue, version));
        emit(ShoppingCartCurrentState(shoppingCartValue,  version));
      });
    });
  }


  Future<void> getShoppingCart() async {
    version = version + 1;
    emit(CreatingShoppingCart(ShoppingCart.empty(),  version));

    final result = await _getShoppingCart();

    result.fold((failure) {
      if (failure.statusCode == 204) {
        emit(ShoppingCartInitialState(ShoppingCart.empty(),  version));
      } else {
        emit(ShoppingCartError(
            ShoppingCart.empty(),  version, failure.errorMessage));
      }
    }, (value) {

      emit(ShoppingCartCurrentState(value, version));
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
            state.shoppingCard,  version, l.errorMessage)),
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
            state.shoppingCard,  version, l.errorMessage)),
        (r) async {});
  }

  Future<void> completePurchase() async {
    var result = await _reserveSeatsUseCase();

    result.fold(
        (l) => emit(ShoppingCartError(
            state.shoppingCard,  version, l.errorMessage)),
        (r) async {});
  }

  @override
  Future<void> close() async {
    await _streamSubscription.cancel();
    return await super.close();
  }
}
