import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/select_seat.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../data/models/seat_info_dto.dart';
import '../entities/shopping_cart.dart';
import '../repos/shopping_cart_local_repo.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dartz/dartz.dart';

class UnselectSeatUseCase
    extends FutureUsecaseWithParams<void, SelectSeatCommand> {
  const UnselectSeatUseCase(this._repo, this._localRepo,);

  final storage = const FlutterSecureStorage();
  final ShoppingCartRepo _repo;
  final ShoppingCartLocalRepo _localRepo;

  @override
  ResultFuture<void> call(SelectSeatCommand params) async {
    var shoppingCartResult = await _localRepo.getShoppingCart();
    return shoppingCartResult.fold((l) => Left(l), (shoppingCart) {
      if (shoppingCart.status != ShoppingCartStatus.InWork) {
        return const Right(null);
      }

      var resultAddSeat = shoppingCart.deleteSeat(params.seat);

      return resultAddSeat.fold(
        (l) => Left(l),
        (r) => _repo.unselectSeat(
          SeatInfoDto(
              row: params.seat.seatRow!,
              number: params.seat.seatNumber!,
              showtimeId: params.movieSessionId,
              shoppingCartId: shoppingCart.id!),
        ),
      );
    });
  }
}
