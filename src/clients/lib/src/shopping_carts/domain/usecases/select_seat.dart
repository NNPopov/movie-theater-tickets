import 'package:equatable/equatable.dart';
import '../../../../core/buses/event_bus.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../hub/domain/event_hub.dart';
import '../../data/models/seat_info_dto.dart';
import '../../data/models/select_seat_dto.dart';
import '../../presentation/cubit/shopping_cart_cubit.dart';
import '../entities/seat.dart';
import '../entities/shopping_cart.dart';
import '../repos/shopping_cart_local_repo.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:dartz/dartz.dart';

class SelectSeatUseCase
    extends FutureUsecaseWithParams<void, SelectSeatCommand> {
  const SelectSeatUseCase(this._repo, this._localRepo, this._eventBus);

  final ShoppingCartRepo _repo;
  final ShoppingCartLocalRepo _localRepo;
  final EventBus _eventBus;

  @override
  ResultFuture<void> call(SelectSeatCommand params) async {
    var shoppingCartResult = await _localRepo.getShoppingCart();


    return shoppingCartResult.fold((l) => Left(l), (shoppingCart) {
      if (shoppingCart.status != ShoppingCartStatus.InWork) {
        return const Right(null);
      }

      params.seat.isDirty = true;
      var resultAddSeat = shoppingCart.addSeat(params.seat);

      return resultAddSeat.fold(
        (l) => Left(l),
        (r) async {

          _eventBus.send(ShoppingCartUpdateEvent(shoppingCart));
         return  _repo.selectSeat(
            SeatInfoDto(
                row: params.seat.seatRow!,
                number: params.seat.seatNumber!,
                showtimeId: params.movieSessionId,
                shoppingCartId: shoppingCart.id!),
            );
        }
      );
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
