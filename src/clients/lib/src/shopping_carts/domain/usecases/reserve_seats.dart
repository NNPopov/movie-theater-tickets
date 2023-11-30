import '../../../../core/common/usecase.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../auth/domain/abstraction/auth_statuses.dart';
import '../../../auth/domain/services/auth_service.dart';
import '../repos/shopping_cart_local_repo.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dartz/dartz.dart';

class ReserveSeatsUseCase extends FutureUsecaseWithoutParams<void> {
  const ReserveSeatsUseCase(
      this._repo, this._localRepo, this._authService);

  final storage = const FlutterSecureStorage();
  final ShoppingCartRepo _repo;
  final ShoppingCartLocalRepo _localRepo;
  final AuthService _authService;

  @override
  ResultFuture<void> call() async {
    var userStatus = await _authService.getCurrentStatus();

    return userStatus.fold((l) => Left(l), (r) async {
      if (r.status != AuthenticationStatus.authorized) {
        return const Left(NotAuthorisedException(message: 'NotAuthorised', statusCode: 401));
      }

      var shoppingCartResult = await _localRepo.getShoppingCart();

      return shoppingCartResult.fold((l) => Left(l), (shoppingCart) async {
        if (shoppingCart.isAssigned!) {
          var result = await _repo.reserveSeats(shoppingCart.id!);
          return result;
        }

        return const Left(ShoppingCartNotAssignedException(
            message: 'Shopping cart is not assigned to user', statusCode: 400));
    });


    });
  }
}
