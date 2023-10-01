import 'package:equatable/equatable.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../repos/shopping_cart_repo.dart';

class CreateShoppingCart
    extends FutureUsecaseWithParams<String, CreateShoppingCartCommand> {
  const CreateShoppingCart(this._repo);

  final ShoppingCartRepo _repo;

  @override
  ResultFuture<String> call(CreateShoppingCartCommand params) {
    var result = _repo.createShoppingCart(params.maxNumberOfSeats);

    return result;
  }
}

class CreateShoppingCartCommand extends Equatable {
  const CreateShoppingCartCommand({required this.maxNumberOfSeats});

  const CreateShoppingCartCommand.empty() : maxNumberOfSeats = 0;

  final int maxNumberOfSeats;

  @override
  List<String> get props => [maxNumberOfSeats.toString()];
}
