import 'package:equatable/equatable.dart';
import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../helpers/constants.dart';
import '../entities/shopping_cart.dart';
import '../repos/shopping_cart_local_repo.dart';
import '../repos/shopping_cart_repo.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class GetShoppingCart extends FutureUsecaseWithParams<ShoppingCart, String> {
  const GetShoppingCart(this._repo, this._localRepo);

  final ShoppingCartRepo _repo;
  final ShoppingCartLocalRepo _localRepo;
  final storage = const FlutterSecureStorage();

  @override
  ResultFuture<ShoppingCart> call(String params) async {
    var result = await _repo.getShoppingCart(params);

    result.fold((l) async {
      if (l.statusCode == 204) {
        await storage.delete(key: Constants.SHOPPING_CARD_ID);
        await storage.delete(key: Constants.SHOPPING_CARD_HASH_ID);
      }
    }, (r) => {_localRepo.setShoppingCart(r)});

    return result;
  }
}
