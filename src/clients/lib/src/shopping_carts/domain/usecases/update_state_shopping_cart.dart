import '../../../../core/common/usecase.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../../core/buses/event_bus.dart';
import '../../presentation/cubit/shopping_cart_cubit.dart';
import '../entities/shopping_cart.dart';
import 'package:dartz/dartz.dart';

import 'package:get_it/get_it.dart';

import '../repos/shopping_cart_local_repo.dart';

GetIt getIt = GetIt.instance;

class UpdateShoppingCartState
    extends FutureUsecaseWithParams<bool, ShoppingCart> {
  UpdateShoppingCartState(
      {EventBus? eventBus, ShoppingCartLocalRepo? localRepo})
      : _eventBus = eventBus ?? getIt.get<EventBus>(),
        _localRepo = localRepo ?? getIt.get<ShoppingCartLocalRepo>();

  late final EventBus _eventBus;
  late final ShoppingCartLocalRepo _localRepo;

  @override
  ResultFuture<bool> call(ShoppingCart params) async {
    try {

      await _localRepo.setShoppingCart(params);
      _eventBus.send(ShoppingCartUpdateEvent(params));


      return const Right(true);
    } on Exception catch (e) {
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    }
  }
}
