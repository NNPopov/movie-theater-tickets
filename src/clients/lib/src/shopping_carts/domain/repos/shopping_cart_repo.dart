import '../../../../core/utils/typedefs.dart';
import '../entities/create_shopping_cart_response.dart';
import '../entities/seat.dart';
import '../entities/shopping_cart.dart';

abstract class ShoppingCartRepo {
  const ShoppingCartRepo();

  ResultFuture<CreateShoppingCartResponse> createShoppingCart(int maxNumberOfSeats);

  ResultFuture<ShoppingCart> getShoppingCart(String shoppingCartId);


  ResultFuture<void>assignClient(String shoppingCartId);

  ResultFuture<void>reserveSeats(String shoppingCartId);

  ResultFuture<void> selectSeat(
      ShoppingCart shoppingCart, ShoppingCartSeat seat, String movieSessionId);

  ResultFuture<void> unselectSeat(
      ShoppingCart shoppingCart, ShoppingCartSeat seat, String movieSessionId);
}
