import 'package:equatable/equatable.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../helpers/constants.dart';
import '../entities/seat.dart';
import '../entities/shopping_cart.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class SelectSeatUseCase extends FutureUsecaseWithParams<void, SelectSeatCommand> {
  const SelectSeatUseCase(this._repo);

  final storage = const FlutterSecureStorage();
  final ShoppingCartRepo _repo;

  @override
  ResultFuture<void> call(SelectSeatCommand params) async {

    //var shoppingCartId = await storage.read(key: Constants.SHOPPING_CARD_ID);
   return _repo.selectSeat(params.shoppingCart, params.seat, params.movieSessionId);
  }
}

class SelectSeatCommand extends Equatable {
  const SelectSeatCommand({required this.seat, required this.shoppingCart, required  this.movieSessionId});


  final ShoppingCartSeat seat;
  final ShoppingCart shoppingCart;
  final String movieSessionId;
  @override
  List<Object?> get props => [ seat, shoppingCart];
}
