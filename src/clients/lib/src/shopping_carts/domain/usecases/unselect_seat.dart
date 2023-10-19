import 'package:equatable/equatable.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/usecases/select_seat.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../helpers/constants.dart';
import '../entities/seat.dart';
import '../entities/shopping_cart.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class UnselectSeatUseCase extends FutureUsecaseWithParams<void, SelectSeatCommand> {
  const UnselectSeatUseCase(this._repo);

  final storage = const FlutterSecureStorage();
  final ShoppingCartRepo _repo;

  @override
  ResultFuture<void> call(SelectSeatCommand params) async {

    //var shoppingCartId = await storage.read(key: Constants.SHOPPING_CARD_ID);
   return _repo.unselectSeat(params.shoppingCart, params.seat, params.movieSessionId);
  }
}

