import 'package:movie_theater_tickets/core/errors/failures.dart';
import '../../../../core/buses/event_bus.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../auth/domain/abstraction/auth_statuses.dart';
import '../../../auth/domain/services/auth_service.dart';
import '../../../helpers/constants.dart';
import '../../../hub/app_events.dart';
import '../../../hub/domain/event_hub.dart';
import '../entities/shopping_cart.dart';
import '../repos/shopping_cart_local_repo.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dartz/dartz.dart';

class GetShoppingCartUseCase extends FutureUsecaseWithoutParams<ShoppingCart> {
  const GetShoppingCartUseCase(this._repo, this._localRepo, this._authService,
      this._eventHub, this._eventBus);

  final ShoppingCartRepo _repo;
  final ShoppingCartLocalRepo _localRepo;
  final AuthService _authService;
  final EventHub _eventHub;
  final EventBus _eventBus;
  final storage = const FlutterSecureStorage();

  @override
  ResultFuture<ShoppingCart> call() async {
    var userStatus = await _authService.getCurrentStatus();

    return userStatus.fold((l) async {
      var shoppingCartId = await storage.read(key: Constants.SHOPPING_CARD_ID);
      if (shoppingCartId != null) {
        await _eventHub.shoppingCartUpdateSubscribe(shoppingCartId);
        return await GetShoppingCartById(shoppingCartId);
      }
      return const Left(
          NotFoundFailure(message: 'ShoppingCart not found', statusCode: 204));
    }, (r) async {
      if (r.status == AuthenticationStatus.authorized) {
        var result = await _repo.getCurrentUserShoppingCart();
        return result.fold((l) {
          return Left(l);
        }, (value) async {
          await storage.write(
              key: Constants.SHOPPING_CARD_ID, value: value.shoppingCartId);
          await storage.write(
              key: Constants.SHOPPING_CARD_HASH_ID, value: value.hashId);

          _eventBus.send(ShoppingCartHashIdUpdated());
          await _eventHub.shoppingCartUpdateSubscribe(value.shoppingCartId);
          return await GetShoppingCartById(value.shoppingCartId);
        });
      }

      var shoppingCartId = await storage.read(key: Constants.SHOPPING_CARD_ID);
      if (shoppingCartId != null) {
        return await GetShoppingCartById(shoppingCartId);
      }
      return const Left(
          NotFoundFailure(message: 'ShoppingCart not found', statusCode: 204));
    });
  }

  ResultFuture<ShoppingCart> GetShoppingCartById(String shoppingCartId) async {
    var result = await _repo.getShoppingCart(shoppingCartId);

    result.fold((l) async {
      if (l.statusCode == 204) {
        await storage.delete(key: Constants.SHOPPING_CARD_ID);
        await storage.delete(key: Constants.SHOPPING_CARD_HASH_ID);
        _eventBus.send(ShoppingCartHashIdUpdated());
      }
    }, (r) => {_localRepo.setShoppingCart(r)});

    return result;
  }
}
