import '../../../../core/utils/typedefs.dart';
import '../entities/shopping_cart.dart';

abstract class ShoppingCartLocalRepo {
  const ShoppingCartLocalRepo();

  ResultFuture<ShoppingCart> getShoppingCart();

  ResultFuture<void> setShoppingCart(ShoppingCart shoppingCart);

  ResultFuture<void> deleteShoppingCart(ShoppingCart shoppingCart);
}
