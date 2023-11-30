import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../hub/domain/event_hub.dart';
import 'package:get_it/get_it.dart';
import 'package:dartz/dartz.dart';

GetIt getIt = GetIt.instance;

class ShoppingCartUpdateSubscribeUseCase
    extends FutureUsecaseWithParams<bool, String> {
  ShoppingCartUpdateSubscribeUseCase({EventHub? eventHub}) {
    _eventHub = eventHub ?? getIt.get<EventHub>();
  }

  late EventHub _eventHub;

  @override
  ResultFuture<bool> call(String params) async {
    await _eventHub.shoppingCartUpdateSubscribe(params);

    return const Right(true);
  }
}
