import '../../../../core/utils/typedefs.dart';
import '../entities/create_shopping_cart_response.dart';
import '../entities/seat.dart';
import '../entities/shopping_cart.dart';

abstract class ShoppingCartLocalRepo {
  const ShoppingCartLocalRepo();

  ResultFuture<ShoppingCart> getShoppingCart();

  ResultFuture<void> setShoppingCart(ShoppingCart shoppingCart);

  ResultFuture<void> deleteShoppingCart(ShoppingCart shoppingCart);
}
