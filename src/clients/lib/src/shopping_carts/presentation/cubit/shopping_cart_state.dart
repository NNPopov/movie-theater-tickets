part of 'shopping_cart_cubit.dart';

@immutable
class ShoppingCartState extends Equatable {
  ShoppingCartState(
      {required this.shoppingCart,
      required this.hashId,
      required this.status,
      this.errorMessage});

  final String hashId;
  final ShoppingCart shoppingCart;
  final ShoppingCartStateStatus status;

  @override
  List<Object> get props => [shoppingCart, hashId, status];

  late String? errorMessage;

  ShoppingCartState copyWith({
    String? hashId,
    ShoppingCart? shoppingCart,

    ShoppingCartStateStatus? status,
    String? errorMessage,
  }) {
    return ShoppingCartState(
        shoppingCart: shoppingCart ?? this.shoppingCart,
        hashId: hashId ?? this.hashId,
        status: status ?? this.status,
        errorMessage: errorMessage);
  }

  static ShoppingCartState initState() {
    return ShoppingCartState(
      shoppingCart: ShoppingCart.empty(),
      hashId: '',
      status: ShoppingCartStateStatus.initial,
    );
  }

  static ShoppingCartState deletedState() {
    return  initState().copyWith(status: ShoppingCartStateStatus.deleted);
  }
}

enum ShoppingCartStateStatus {
  initial,
  creating,
  created,
  error,
  createValidationError,
  deleted,
  update
}
