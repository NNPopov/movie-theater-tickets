import '../../../../core/utils/typedefs.dart';
import '../entities/seat.dart';
import '../entities/shopping_cart.dart';

abstract class ShoppingCartRepo {
  const ShoppingCartRepo();

  ResultFuture<String> createShoppingCart(int maxNumberOfSeats);

  ResultFuture<ShoppingCart> getShoppingCart(String shoppingCartId);

  ResultFuture<void> selectSeat(ShoppingCart shoppingCart, Seat seat);
}