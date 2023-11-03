import 'dart:async';

import 'package:get_it/get_it.dart';
import '../../../../core/buses/event_bus.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../auth/domain/abstraction/auth_event_bus.dart';
import '../../../auth/domain/services/auth_service.dart';
import '../entities/create_shopping_cart_response.dart';
import '../repos/shopping_cart_local_repo.dart';
import '../usecases/assign_client_use_case.dart';
import '../usecases/create_shopping_cart.dart';
import '../usecases/create_shopping_cart.dart';
import '../usecases/get_shopping_cart.dart';
import '../usecases/reserve_seats.dart';
import '../usecases/select_seat.dart';
import '../usecases/shopping_cart_subscribe.dart';
import '../usecases/unselect_seat.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dartz/dartz.dart';

GetIt getIt = GetIt.instance;

class ShoppingCartService {
  ShoppingCartService(
      {ShoppingCartLocalRepo? localRepo,
      CreateShoppingCartUseCase? createShoppingCartUseCase,
      SelectSeatUseCase? selectSeatUseCase,
      UnselectSeatUseCase? unselectSeatUseCase,
      GetShoppingCart? getShoppingCartUseCase,
      ShoppingCartUpdateSubscribeUseCase? shoppingCartUpdateSubscribeUseCase,
      AssignClientUseCase? assignClientUseCase,
      ReserveSeatsUseCase? reserveSeatsUseCase,
      EventBus? eventBus,
      AuthService? authService,
      AuthEventBus? authEventBus})
      : _createShoppingCart =
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
        _authService = authService ?? getIt.get<AuthService>(),
        _authEventBus = authEventBus ?? getIt<AuthEventBus>(),
        _localRepo = localRepo ?? getIt<ShoppingCartLocalRepo>();

  late final StreamSubscription _appEventSubscription;
  late final StreamSubscription _streamSubscription;
  final storage = const FlutterSecureStorage();

  late final ShoppingCartLocalRepo _localRepo;
  late final AuthEventBus _authEventBus;
  late final AuthService _authService;
  late final EventBus _eventBus;
  late final CreateShoppingCartUseCase _createShoppingCart;
  late final SelectSeatUseCase _selectSeatUseCase;
  late final UnselectSeatUseCase _unselectSeatUseCase;
  late final GetShoppingCart _getShoppingCart;
  late final ShoppingCartUpdateSubscribeUseCase
      _shoppingCartUpdateSubscribeUseCase;
  late final ReserveSeatsUseCase _reserveSeatsUseCase;

  late final AssignClientUseCase _assignClientUseCase;

  Future<void> init() async {
    _appEventSubscription = _authEventBus.stream.listen((event) async {
      if (event is AuthorizedAuthStatus) {
        var shoppingCartResult = await _localRepo.getShoppingCart();

        shoppingCartResult.fold((l) => null, (r) async {
          var assignClientResult = await _assignClientUseCase(r.id!);

          assignClientResult.fold((l) => null, (r) async {
            return const Right(null);
          });
        });
      }
    });
  }

  ResultFuture<CreateShoppingCartResponse> createShoppingCart(
      int maxNumberOfSeats) async {
    var createShoppingCartCommand =
        CreateShoppingCartCommand(maxNumberOfSeats: maxNumberOfSeats);

    final result = await _createShoppingCart(createShoppingCartCommand);

    result.fold((failure) => Left(failure), (value) async {
      var shoppingCartResult = await _getShoppingCart(value.shoppingCartId);

      shoppingCartResult.fold((l) => null, (shoppingCart) async {
        await _localRepo.setShoppingCart(shoppingCart);
        var authStatus = await _authService.getCurrentStatus();

        authStatus.fold((l) => null, (r) async {
          if (r is AuthorizedAuthStatus) {
            var assignClientResult =
                await _assignClientUseCase(value.shoppingCartId);

            assignClientResult.fold((l) => null, (r) async {});
          }
        });
      });
    });

    return result;
  }
}
