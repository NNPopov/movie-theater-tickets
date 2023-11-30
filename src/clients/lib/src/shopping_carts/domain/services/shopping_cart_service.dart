import 'dart:async';
import '../../../../core/buses/event_bus.dart';
import '../../../auth/domain/abstraction/auth_statuses.dart';
import '../../../auth/domain/services/auth_service.dart';
import '../../../helpers/constants.dart';
import '../../../hub/app_events.dart';
import '../../../hub/domain/event_hub.dart';
import '../../presentation/cubit/shopping_cart_cubit.dart';
import '../repos/shopping_cart_local_repo.dart';
import '../usecases/assign_client_use_case.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dartz/dartz.dart';

import '../usecases/get_shopping_cart.dart';

class ShoppingCartAuthListener {
  ShoppingCartAuthListener(
      this._localRepo, this._assignClientUseCase, this._authService, this._eventHub, this._eventBus, this._getShoppingCartUseCase);

  final storage = const FlutterSecureStorage();

  final EventHub _eventHub;
  final EventBus _eventBus;

  final ShoppingCartLocalRepo _localRepo;

  late final AssignClientUseCase _assignClientUseCase;
  late final GetShoppingCartUseCase _getShoppingCartUseCase;
  late final StreamSubscription<AuthStatus> _authenticationStatusSubscription;

  final AuthService _authService;

  Future<void> init() async {
    _authenticationStatusSubscription =
        _authService.status.listen((event) async {
      if (event.status == AuthenticationStatus.authorized) {
        var shoppingCartResult = await _localRepo.getShoppingCart();

        shoppingCartResult.fold((l) async {

        await  _getShoppingCartUseCase();



        _eventBus.send(const ShoppingCartHashIdIdUpdateEvent());

        }, (r) async {
          var assignClientResult = await _assignClientUseCase(r.id!);



          assignClientResult.fold((l) => null, (r) async {


            return const Right(null);
          });
        });
      }

      if (event.status == AuthenticationStatus.unauthorized) {
        var shoppingCartResult = await _localRepo.getShoppingCart();

        shoppingCartResult.fold((l) {}, (shoppingCart) async {
          await _localRepo.deleteShoppingCart(shoppingCart);

          _eventBus.send(ShoppingCartHashIdUpdated());

          _eventHub.shoppingCartRemoveSubscribe(shoppingCart.id!);

          return const Right(null);
        });

        await storage.delete(key: Constants.SHOPPING_CARD_ID);
        await storage.delete(key: Constants.SHOPPING_CARD_HASH_ID);

        _eventBus.send(ShoppingCartDeleteEvent());
      }
    });
  }
}
