part of 'shopping_cart_cubit.dart';

abstract class ShoppingCartState extends Equatable {
  const ShoppingCartState(this.shoppingCard,
   this.version, this.hashId,);

  final String hashId;
  final ShoppingCart shoppingCard;
  final int version;

  @override
  List<Object> get props => [shoppingCard, version, hashId];
}


class CreatingShoppingCart extends ShoppingCartState {
  const CreatingShoppingCart(super.shoppingCard, super.version, super.hashId);
}

class ShoppingCartCurrentState extends ShoppingCartState {
  const ShoppingCartCurrentState(
      super.shoppingCard, super.version, super.hashId);
}

class ShoppingCartConflictState extends ShoppingCartState {
  const ShoppingCartConflictState(
      super.shoppingCard, super.version, super.hashId);


}

class ShoppingCartCreatedState extends ShoppingCartState {
  const ShoppingCartCreatedState(
      super.shoppingCard, super.version, super.hashId);


}

class ShoppingCartInitialState extends ShoppingCartState {
  const ShoppingCartInitialState(
      super.shoppingCard,super.version, super.hashId);
}
class ShoppingCartDeleteState extends ShoppingCartState {
  const ShoppingCartDeleteState(
      super.shoppingCard,super.version, super.hashId);
}


class ShoppingCartCreateValidationErrorState extends ShoppingCartError {
  const ShoppingCartCreateValidationErrorState(
      super.shoppingCard, super.version, super.hashId,  super.message);
}

class ShoppingCartError extends ShoppingCartState {
  const ShoppingCartError(
      super.shoppingCard, super.version, super.hashId, this.message);

  final String message;

  @override
  List<Object> get props => [shoppingCard, version, hashId, message];
}
