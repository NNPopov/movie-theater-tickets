import '../../../../core/common/usecase.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../../core/buses/event_bus.dart';
import '../../presentation/cubit/shopping_cart_cubit.dart';
import '../entities/shopping_cart.dart';
import 'package:dartz/dartz.dart';

import '../repos/shopping_cart_local_repo.dart';

class ShoppingCartUpdateStateUseCase
    extends FutureUsecaseWithParams<bool, ShoppingCart> {
  ShoppingCartUpdateStateUseCase(this._eventBus, this._localRepo);

  final EventBus _eventBus;
  final ShoppingCartLocalRepo _localRepo;

  @override
  ResultFuture<bool> call(ShoppingCart params) async {
    try {
      if (params.status == ShoppingCartStatus.Deleted) {
        await _localRepo.deleteShoppingCart(params);

        _eventBus.send(ShoppingCartDeleteEvent());
      } else {
        await _localRepo.setShoppingCart(params);
        _eventBus.send(ShoppingCartUpdateEvent(params));
      }

      return const Right(true);
    } on Exception catch (e) {
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    }
  }
}
