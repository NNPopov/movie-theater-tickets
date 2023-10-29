import 'package:dartz/dartz.dart';
import 'package:movie_theater_tickets/core/utils/typedefs.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/entities/shopping_cart.dart';
import '../../domain/repos/shopping_cart_local_repo.dart';
import '../models/shopping_cart_dto.dart';
import 'package:get_it/get_it.dart';
import 'package:localstorage/localstorage.dart';


GetIt getIt = GetIt.instance;

class ShoppingCartLocalRepoImpl implements ShoppingCartLocalRepo {

  final LocalStorage storage = LocalStorage('movie_theatre.json');
  ShoppingCartRepoLocalImpl() {
  }

  @override
  ResultFuture<ShoppingCart> getShoppingCart() async {
    var shoppingCartDto = storage.getItem('ShoppingCart');
    if (shoppingCartDto == null)
      {
        return const Left(DataFailure( message: 'ShoppingCart not stored', statusCode: 404));
      }

    var shoppingCart = ShoppingCartDto.fromJson(shoppingCartDto);

    return Right(shoppingCart);
  }

  @override
  ResultFuture<void> setShoppingCart(ShoppingCart shoppingCart) async {

    var shoppingCartDto = shoppingCart.map();
    
    
     storage.setItem('ShoppingCart', shoppingCartDto.toJson());
    
  return  const Right(null);
  }

}
