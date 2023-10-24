import 'package:equatable/equatable.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../entities/shopping_cart.dart';
import '../repos/shopping_cart_local_repo.dart';
import '../repos/shopping_cart_repo.dart';

class GetShoppingCart
    extends FutureUsecaseWithParams<ShoppingCart, String> {
  const GetShoppingCart(this._repo, this._localRepo);

  final ShoppingCartRepo _repo;
  final ShoppingCartLocalRepo _localRepo;

  @override
  ResultFuture<ShoppingCart> call(String params) async {

    var result = await _repo.getShoppingCart(params);

    result.fold((l) => null, (r) => {
      _localRepo.setShoppingCart(r)
    });

    return result;
  }
}