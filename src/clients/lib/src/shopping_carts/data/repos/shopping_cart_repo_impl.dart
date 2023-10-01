import 'dart:convert';
import 'package:dartz/dartz.dart';
import 'package:movie_theater_tickets/core/utils/typedefs.dart';
import 'package:dio/dio.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/entities/seat.dart';
import '../../domain/entities/shopping_cart.dart';
import '../../domain/repos/shopping_cart_repo.dart';
import '../models/create_shopping_cart_dto.dart';
import '../models/shopping_cart_dto.dart';
import 'package:flutter_guid/flutter_guid.dart';

class ShoppingCartRepoImpl extends ShoppingCartRepo {
  final Dio _client;

  ShoppingCartRepoImpl(this._client);

  @override
  ResultFuture<String> createShoppingCart(int maxNumberOfSeats) async {
    try {
      CreateShoppingCartDto request = CreateShoppingCartDto(maxNumberOfSeats);

      final response = await _client.post('/api/shoppingcarts',
          data: request.toJson(),
          options:
              Options(headers: {'X-Idempotency-Key': Guid.newGuid.toString()}));

      return Right(response.toString());
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message, statusCode: e.statusCode));
    }
  }

  @override
  ResultFuture<ShoppingCart> getShoppingCart(String shoppingCartId) async {
    try {
      final response = await _client.get('/api/shoppingcarts/$shoppingCartId');
      var primaryClientAccount = json.decode(response.toString());

      var shoppingCartDto = ShoppingCartDto.fromJson(primaryClientAccount);

      return Right(shoppingCartDto);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message, statusCode: e.statusCode));
    }
  }

  @override
  ResultFuture<void> selectSeat(ShoppingCart shoppingCart, Seat seat) {
    // TODO: implement selectSeat
    throw UnimplementedError();
  }
}
