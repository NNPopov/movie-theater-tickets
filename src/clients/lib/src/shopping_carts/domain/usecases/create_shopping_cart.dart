import 'package:equatable/equatable.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../auth/domain/abstraction/auth_statuses.dart';
import '../../../auth/domain/services/auth_service.dart';
import '../../../helpers/constants.dart';
import '../../../hub/domain/event_hub.dart';
import '../entities/create_shopping_cart_response.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dartz/dartz.dart';
import '../services/shopping_cart_service.dart';

GetIt getIt = GetIt.instance;

class CreateShoppingCartUseCase
    extends FutureUsecaseWithParams<String, CreateShoppingCartCommand> {
  CreateShoppingCartUseCase(this._service, this._eventHub, this._authService, this._repo);

  final storage = const FlutterSecureStorage();
  final ShoppingCartAuthListener _service;
  final EventHub _eventHub;
  final AuthService _authService;
  final ShoppingCartRepo _repo;

  @override
  ResultFuture<String> call(CreateShoppingCartCommand params) async {
    var userStatus = await _authService.getCurrentStatus();

    return userStatus
        .fold((l) async => await createShoppingCartForAnonymousUser(params),
            (r) async {
      if (r.status == AuthenticationStatus.authorized) {
        return await createShoppingCartForNotAnonymousUser(params);
      }

      return await createShoppingCartForAnonymousUser(params);
    });
  }

  ResultFuture<String> createShoppingCartForNotAnonymousUser(
      CreateShoppingCartCommand params) async {
    var result =
        await createShoppingCartForAnonymousUser1(params.maxNumberOfSeats);

    return result.fold((l) {
      return Left(l);
    }, (value) async {
      await storage.write(
          key: Constants.SHOPPING_CARD_ID, value: value.shoppingCartId);
      await storage.write(
          key: Constants.SHOPPING_CARD_HASH_ID, value: value.hashId);

      await _eventHub.shoppingCartUpdateSubscribe(value.shoppingCartId);

      var shoppingCartResult =
          await _repo.getShoppingCart(value.shoppingCartId);

      return shoppingCartResult.fold((l) => Left(l), (r) async {
        await _repo.assignClient(value.shoppingCartId);



        return Right(value.hashId);
      });
    });
  }

  ResultFuture<String> createShoppingCartForAnonymousUser(
      CreateShoppingCartCommand params) async {
    var result =
        await createShoppingCartForAnonymousUser1(params.maxNumberOfSeats);

    return result.fold((l) {
      return Left(l);
    }, (value) async {
      await storage.write(
          key: Constants.SHOPPING_CARD_ID, value: value.shoppingCartId);
      await storage.write(
          key: Constants.SHOPPING_CARD_HASH_ID, value: value.hashId);

      await _eventHub.shoppingCartUpdateSubscribe(value.shoppingCartId);

      // var shoppingCartResult =
      //     await _repo.getShoppingCart(value.shoppingCartId);

      return Right(value.hashId);
    });
  }

  ResultFuture<CreateShoppingCartResponse> createShoppingCartForAnonymousUser1(
      int maxNumberOfSeats) async {
    if (maxNumberOfSeats > 4 || maxNumberOfSeats < 1) {
      return const Left(
          ValidationFailure(message: 'Number of places should be from 1 to 4'));
    }

    return await _repo.createShoppingCart(maxNumberOfSeats);
  }
}

class CreateShoppingCartCommand extends Equatable {
  const CreateShoppingCartCommand({required this.maxNumberOfSeats});

  final int maxNumberOfSeats;

  @override
  List<String> get props => [maxNumberOfSeats.toString()];
}
