import 'package:equatable/equatable.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../helpers/constants.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dartz/dartz.dart';

GetIt getIt = GetIt.instance;

class CreateShoppingCart
    extends FutureUsecaseWithParams<String, CreateShoppingCartCommand> {
  CreateShoppingCart({ShoppingCartRepo? repo}) {
    _repo = repo ?? getIt.get<ShoppingCartRepo>();
  }

  final storage = const FlutterSecureStorage();
  late ShoppingCartRepo _repo;

  @override
  ResultFuture<String> call(CreateShoppingCartCommand params) async {
    var result =  await _repo.createShoppingCart(params.maxNumberOfSeats);

    result.fold(
            (_) =>{},
            (value)  async {
              storage.write(key: Constants.SHOPPING_CARD_ID, value: value);
        });





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
