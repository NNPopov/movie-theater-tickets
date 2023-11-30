part of 'shopping_cart_cubit.dart';

abstract class ShoppingCartEvent extends Equatable {
  const ShoppingCartEvent();

  @override
  List<Object> get props => [];
}

class ShoppingCartHashIdIdUpdateEvent extends ShoppingCartEvent {
  const ShoppingCartHashIdIdUpdateEvent();

  @override
  List<Object> get props => [];
}

class ShoppingCartUpdateEvent extends ShoppingCartEvent {
  const ShoppingCartUpdateEvent(this.shoppingCart);

  final ShoppingCart shoppingCart;

  @override
  List<Object> get props => [shoppingCart];
}

class ShoppingCartDeleteEvent extends ShoppingCartEvent {

}

