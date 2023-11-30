import 'package:equatable/equatable.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../entities/seat.dart';
import '../entities/shopping_cart.dart';
import '../repos/shopping_cart_local_repo.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:dartz/dartz.dart';

class SelectSeatUseCase
    extends FutureUsecaseWithParams<void, SelectSeatCommand> {
  const SelectSeatUseCase(this._repo, this._localRepo);

  final ShoppingCartRepo _repo;
  final ShoppingCartLocalRepo _localRepo;

  @override
  ResultFuture<void> call(SelectSeatCommand params) async {
    var shoppingCartResult = await _localRepo.getShoppingCart();

    return shoppingCartResult.fold((l) => Left(l), (shoppingCart) {
      if (shoppingCart.status != ShoppingCartStatus.InWork) {
        return const Right(null);
      }
      var resultAddSeat = shoppingCart.addSeat(params.seat);

      return resultAddSeat.fold(
          (l) => Left(l),
          (r) => _repo.selectSeat(
              shoppingCart, params.seat, params.movieSessionId));
    });
  }
}

class SelectSeatCommand extends Equatable {
  const SelectSeatCommand({required this.seat, required this.movieSessionId});

  final ShoppingCartSeat seat;
  final String movieSessionId;

  @override
  List<Object?> get props => [seat, movieSessionId];
}
