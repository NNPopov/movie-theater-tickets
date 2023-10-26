import 'package:equatable/equatable.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../helpers/constants.dart';
import '../../../hub/domain/event_hub.dart';
import '../../../../core/buses/event_bus.dart';
import '../entities/create_shopping_cart_response.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dartz/dartz.dart';
import 'package:movie_theater_tickets/core/errors/failures.dart';

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
