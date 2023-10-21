part of 'shopping_cart_cubit.dart';

abstract class ShoppingCartState  extends Equatable {
const ShoppingCartState();

  @override
  List<Object> get props => [];

}


// class SelectingSeat extends ShoppingCartState {
//   const SelectingSeat(this.seat);
//
//   final ShoppingCartSeat seat;
//
//   @override
//   List<Object> get props => [seat];
// }

class CreatingShoppingCart extends ShoppingCartState {
  const CreatingShoppingCart();
}

class GettingShoppingCart extends ShoppingCartState {
  const GettingShoppingCart();
}

class SelectedSeat extends ShoppingCartState {
  const SelectedSeat(this.seat);

  final ShoppingCartSeat seat;

  @override
  List<Object> get props => [seat];
}

class ShoppingCartCurrentState extends ShoppingCartState  {
const ShoppingCartCurrentState(this.shoppingCard, this.version);

final ShoppingCart shoppingCard;
final int version;
@override
List<Object> get props => [shoppingCard, version];
}

class ShoppingCartConflictState extends ShoppingCartCurrentState  {
  const ShoppingCartConflictState(super.shoppingCard, super.version);

  @override
  List<Object> get props => [shoppingCard, version];
}



class ShoppingCartValue extends ShoppingCartState  {
  const ShoppingCartValue(this.shoppingCard);

  final ShoppingCart shoppingCard;

  @override
  List<Object> get props => [shoppingCard];
}

class ShoppingCartError extends ShoppingCartState {
  const ShoppingCartError(this.message);

  final String message;

  @override
  List<Object> get props => [message];
}

class ShoppingCartInitialState extends ShoppingCartState {
  const ShoppingCartInitialState();
}
