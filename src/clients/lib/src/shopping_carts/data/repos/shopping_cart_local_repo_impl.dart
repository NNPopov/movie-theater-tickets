import 'dart:convert';

import 'package:dartz/dartz.dart';
import 'package:localstorage/localstorage.dart';
import 'package:movie_theater_tickets/core/utils/typedefs.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/entities/shopping_cart.dart';
import '../../domain/repos/shopping_cart_local_repo.dart';
import '../models/shopping_cart_dto.dart';

const _shoppingCartKey = 'ShoppingCart';

class ShoppingCartLocalRepoImpl implements ShoppingCartLocalRepo {
  // localstorage 6 dropped the per-instance `LocalStorage('file.json')`
  // constructor and `.ready` future in favour of a single global store
  // (`window.localStorage`-style) initialised once via `initLocalStorage()`.
  // `getItem`/`setItem` are now synchronous and String-typed, and `deleteItem`
  // became `removeItem`. The adapter initialises lazily before each access and
  // JSON-encodes/decodes the cart DTO itself.
  LocalStorage get storage => localStorage;

  @override
  ResultFuture<ShoppingCart> getShoppingCart() async {
    await initLocalStorage();
    final raw = storage.getItem(_shoppingCartKey);
    if (raw == null) {
      return const Left(
        DataFailure(message: 'ShoppingCart not stored', statusCode: 404),
      );
    }

    final shoppingCart = ShoppingCartDto.fromJson(
      jsonDecode(raw) as Map<String, dynamic>,
    );

    return Right(shoppingCart);
  }

  @override
  ResultFuture<void> setShoppingCart(ShoppingCart shoppingCart) async {
    await initLocalStorage();
    final shoppingCartDto = shoppingCart.map();

    storage.setItem(_shoppingCartKey, jsonEncode(shoppingCartDto.toJson()));

    return const Right(null);
  }

  @override
  ResultFuture<void> deleteShoppingCart(ShoppingCart shoppingCart) async {
    await initLocalStorage();
    storage.removeItem(_shoppingCartKey);

    return const Right(null);
  }
}
