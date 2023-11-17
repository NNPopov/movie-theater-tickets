import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/select_seat.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../auth/domain/abstraction/auth_event_bus.dart';
import '../../../auth/domain/services/auth_service.dart';
import '../repos/shopping_cart_local_repo.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dartz/dartz.dart';

class AssignClientUseCase extends FutureUsecaseWithParams<void, String> {
  const AssignClientUseCase(this._repo, this._localRepo, this._authService);

  final storage = const FlutterSecureStorage();
  final ShoppingCartRepo _repo;
  final ShoppingCartLocalRepo _localRepo;
  final AuthService _authService;

  @override
  ResultFuture<void> call(String shoppingCartId) async {
    var userStatus = await _authService.getCurrentStatus();

    return userStatus.fold((l) => Left(l), (r) async {
      if (r is! AuthorizedAuthStatus) {
        return const Left(
            NotAuthorisedException());
      }

      var shoppingCartResult = await _localRepo.getShoppingCart();
      return shoppingCartResult.fold((l) => Left(l), (shoppingCart) async {
        if (!shoppingCart.isAssigned!) {
          var result = await _repo.assignClient(shoppingCartId);
          return result;
        }
        return const Right(null);
      });
    });
  }
}
