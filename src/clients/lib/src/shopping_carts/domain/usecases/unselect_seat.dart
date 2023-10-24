import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/select_seat.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../repos/shopping_cart_local_repo.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dartz/dartz.dart';


class UnselectSeatUseCase extends FutureUsecaseWithParams<void, SelectSeatCommand> {
  const UnselectSeatUseCase(this._repo, this._localRepo);

  final storage = const FlutterSecureStorage();
  final ShoppingCartRepo _repo;
  final ShoppingCartLocalRepo _localRepo;

  @override
  ResultFuture<void> call(SelectSeatCommand params) async {

    var shoppingCartResult = await _localRepo.getShoppingCart();
    return shoppingCartResult.fold((l) => Left(l), (shoppingCart) {

      var resultAddSeat = shoppingCart.deleteSeat(params.seat);

      // return resultAddSeat.fold(
      //         (l) => Left(l),
      //         (r) => _repo.unselectSeat(
      //         shoppingCart, params.seat, params.movieSessionId));

    return  _repo.unselectSeat(
          shoppingCart, params.seat, params.movieSessionId);
    });
  }
}

