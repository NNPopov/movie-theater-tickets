import 'package:equatable/equatable.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../entities/seat.dart';
import '../entities/shopping_cart.dart';
import '../repos/shopping_cart_repo.dart';

class SelectSeat extends FutureUsecaseWithParams<void, SelectSeatCommand> {
  const SelectSeat(this._repo);

  final ShoppingCartRepo _repo;

  @override
  ResultFuture<void> call(SelectSeatCommand params) {
   return _repo.selectSeat(params.shoppingCart, params.seat);
  }
}

class SelectSeatCommand extends Equatable {
  const SelectSeatCommand({required this.shoppingCart, required this.seat});

  //const SelectSeatCommand.empty() : shoppingCart = ShoppingCart;

  final ShoppingCart shoppingCart;
  final Seat seat;

  @override
  List<Object?> get props => [shoppingCart, seat];
}
