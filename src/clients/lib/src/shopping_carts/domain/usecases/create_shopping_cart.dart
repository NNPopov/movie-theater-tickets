import 'package:equatable/equatable.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../auth/domain/abstraction/auth_event_bus.dart';
import '../../../auth/domain/services/auth_service.dart';
import '../../../helpers/constants.dart';
import '../../../hub/domain/event_hub.dart';
import '../entities/create_shopping_cart_response.dart';
import '../entities/shopping_cart.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dartz/dartz.dart';
import '../services/shopping_cart_service.dart';

GetIt getIt = GetIt.instance;

class CreateShoppingCartUseCase
    extends FutureUsecaseWithParams<ShoppingCart, CreateShoppingCartCommand> {
  CreateShoppingCartUseCase(this._repo, this._eventHub, this._authService);

  final storage = const FlutterSecureStorage();
  final ShoppingCartService _repo;
  final EventHub _eventHub;
  final AuthService _authService;

  @override
  ResultFuture<ShoppingCart> call(CreateShoppingCartCommand params) async {
    var userStatus = await _authService.getCurrentStatus();

    return userStatus
        .fold((l) async => await createShoppingCartForAnonymousUser(params),
            (r) async {
      if (r is AuthorizedAuthStatus) {
        return await createShoppingCartForNotAnonymousUser(params);
      }

      return await createShoppingCartForAnonymousUser(params);
    });
  }

  ResultFuture<ShoppingCart> createShoppingCartForNotAnonymousUser(
      CreateShoppingCartCommand params) async {
    var result =
        await _repo.createShoppingCartForAnonymousUser(params.maxNumberOfSeats);

    return result.fold((l) {
      return Left(l);
    }, (value) async {
      await storage.write(
          key: Constants.SHOPPING_CARD_ID, value: value.shoppingCartId);
      await storage.write(
          key: Constants.SHOPPING_CARD_HASH_ID, value: value.hashId);

      var shoppingCartResult =
          await _repo.getShoppingCartById(value.shoppingCartId);

      return shoppingCartResult.fold((l) => Left(l), (r) async {
        await _repo.assignClient(value.shoppingCartId);

        await _eventHub.shoppingCartUpdateSubscribe(value.shoppingCartId);

        return Right(r);
      });
    });
  }

  ResultFuture<ShoppingCart> createShoppingCartForAnonymousUser(
      CreateShoppingCartCommand params) async {
    var result =
        await _repo.createShoppingCartForAnonymousUser(params.maxNumberOfSeats);

    return result.fold((l) {
      return Left(l);
    }, (value) async {
      await storage.write(
          key: Constants.SHOPPING_CARD_ID, value: value.shoppingCartId);
      await storage.write(
          key: Constants.SHOPPING_CARD_HASH_ID, value: value.hashId);

      await _eventHub.shoppingCartUpdateSubscribe(value.shoppingCartId);

      var shoppingCartResult =
          await _repo.getShoppingCartById(value.shoppingCartId);

      return shoppingCartResult;
    });
  }
}

class CreateShoppingCartCommand extends Equatable {
  const CreateShoppingCartCommand({required this.maxNumberOfSeats});

  final int maxNumberOfSeats;

  @override
  List<String> get props => [maxNumberOfSeats.toString()];
}
