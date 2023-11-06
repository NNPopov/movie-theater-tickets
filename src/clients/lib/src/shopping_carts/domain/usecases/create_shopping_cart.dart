import 'package:equatable/equatable.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../helpers/constants.dart';
import '../../../hub/domain/event_hub.dart';
import '../entities/create_shopping_cart_response.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

GetIt getIt = GetIt.instance;

class CreateShoppingCartUseCase extends FutureUsecaseWithParams<
    CreateShoppingCartResponse, CreateShoppingCartCommand> {
  CreateShoppingCartUseCase({ShoppingCartRepo? repo, EventHub? eventHub}) {
    _repo = repo ?? getIt.get<ShoppingCartRepo>();
    _eventHub = eventHub ?? getIt.get<EventHub>();
  }

  final storage = const FlutterSecureStorage();
  late ShoppingCartRepo _repo;
  late EventHub _eventHub;

  @override
  ResultFuture<CreateShoppingCartResponse> call(
      CreateShoppingCartCommand params) async {
    var result = await _repo.createShoppingCart(params.maxNumberOfSeats);

    result.fold((_) => {


    }, (value) async {
      await storage.write(
          key: Constants.SHOPPING_CARD_ID, value: value.shoppingCartId);
      await storage.write(
          key: Constants.SHOPPING_CARD_HASH_ID, value: value.hashId);

      await _eventHub.shoppingCartUpdateSubscribe(value.shoppingCartId);
    });
    return result;
  }
}

class CreateShoppingCartCommand extends Equatable {
  const CreateShoppingCartCommand({required this.maxNumberOfSeats});

  final int maxNumberOfSeats;

  @override
  List<String> get props => [maxNumberOfSeats.toString()];
}
