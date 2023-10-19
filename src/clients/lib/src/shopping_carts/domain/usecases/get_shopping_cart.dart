import 'package:equatable/equatable.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../entities/shopping_cart.dart';
import '../repos/shopping_cart_repo.dart';

class GetShoppingCart
    extends FutureUsecaseWithParams<ShoppingCart, String> {
  const GetShoppingCart(this._repo);

  final ShoppingCartRepo _repo;

  @override
  ResultFuture<ShoppingCart> call(String params) async {

    var result = await _repo.getShoppingCart(params);

    return result;
  }
}