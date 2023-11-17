import 'dart:convert';
import 'package:dartz/dartz.dart';
import 'package:movie_theater_tickets/core/utils/typedefs.dart';
import 'package:dio/dio.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/entities/create_shopping_cart_response.dart';
import '../../domain/entities/seat.dart';
import '../../domain/entities/shopping_cart.dart';
import '../../domain/repos/shopping_cart_repo.dart';
import '../models/create_shopping_cart_dto.dart';
import '../models/select_seat_dto.dart';
import '../models/shopping_cart_dto.dart';
import 'package:flutter_guid/flutter_guid.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class ShoppingCartRepoImpl implements ShoppingCartRepo {
  late Dio _client;

  ShoppingCartRepoImpl({Dio? client}) {
    _client = client ?? getIt.get<Dio>();
  }

  @override
  ResultFuture<CreateShoppingCartResponse> createShoppingCart(
      int maxNumberOfSeats) async {
    try {
      CreateShoppingCartDto request = CreateShoppingCartDto(maxNumberOfSeats);

      final response = await _client.post('/api/shoppingcarts',
          data: request.toJson(),
          options:
              Options(headers: {'X-Idempotency-Key': Guid.newGuid.toString()}));

      var shoppingCart = CreateShoppingCartResponse.fromJson(
          response.data as Map<String, dynamic>) as CreateShoppingCartResponse;

      return Right(shoppingCart);
    } on Exception catch (e) {
      return Left(
          ServerFailure(message: e.toString(), statusCode: e.toString()));
    }
  }

  @override
  ResultFuture<ShoppingCart> getShoppingCart(String shoppingCartId) async {
    try {
      final response = await _client.get('/api/shoppingcarts/$shoppingCartId');

      if (response.statusCode == 204) {
        return const Left(DataFailure(
            message: "shoppingCartId doesn't exist", statusCode: 204));
      }

      var primaryClientAccount = json.decode(response.toString());

      var shoppingCartDto = ShoppingCartDto.fromJson(primaryClientAccount);

      return Right(shoppingCartDto);
    } on Exception catch (e) {
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    }
  }

  @override
  ResultFuture<void> selectSeat(ShoppingCart shoppingCart,
      ShoppingCartSeat seat, String movieSessionId) async {
    try {
      var request = SelectSeatShoppingCartDto(
          row: seat.seatRow!,
          number: seat.seatNumber!,
          showtimeId: movieSessionId);

      final response = await _client.post(
          '/api/shoppingcarts/${shoppingCart.id}/seats/select',
          data: request.toJson(),
          options:
              Options(headers: {'X-Idempotency-Key': Guid.newGuid.toString()}));

      return Right(null);
    } on DioException catch (e) {
      if (e.response?.statusCode == 409) {
        return Left(ConflictFailure(message: e.message!));
      }
      return Left(ServerFailure(message: e.message!, statusCode: 500));
    } on Exception catch (e) {
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    }
  }

  @override
  ResultFuture<void> unselectSeat(ShoppingCart shoppingCart,
      ShoppingCartSeat seat, String movieSessionId) async {
    try {
      var request = SelectSeatShoppingCartDto(
          row: seat.seatRow!,
          number: seat.seatNumber!,
          showtimeId: movieSessionId);

      final response = await _client.delete(
          '/api/shoppingcarts/${shoppingCart.id}/seats/unselect',
          data: request.toJson(),
          options:
              Options(headers: {'X-Idempotency-Key': Guid.newGuid.toString()}));

      return Right(null);
    } on Exception catch (e) {
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    }
  }

  @override
  ResultFuture<CreateShoppingCartResponse> getCurrentUserShoppingCart() async {
    try {
      final response = await _client.get('/api/shoppingcarts/current',
          options:
              Options(headers: {'X-Idempotency-Key': Guid.newGuid.toString()}));

      if (response.statusCode == 204) {
        return Left(NotFoundFailure( statusCode: 204));
      }

      var shoppingCart = CreateShoppingCartResponse.fromJson(
          response.data as Map<String, dynamic>) as CreateShoppingCartResponse;

      return Right(shoppingCart);
    } on DioException catch (e) {
      if (e.response?.statusCode == 409) {
        return Left(ConflictFailure(message: e.toString()));
      }
      if (e.response?.statusCode == 204) {
        return Left(NotFoundFailure(message: e.toString()));
      }
      if (e.response?.statusCode == 404) {
        return Left(NotFoundFailure(message: e.toString()));
      }
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    } on Exception catch (e) {
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    }
  }

  @override
  ResultFuture<void> assignClient(String shoppingCartId) async {
    try {
      final response = await _client.put(
          '/api/shoppingcarts/${shoppingCartId}/assignclient',
          options:
              Options(headers: {'X-Idempotency-Key': Guid.newGuid.toString()}));

      return Right(null);
    } on DioException catch (e) {
      if (e.response?.statusCode == 409) {
        return Left(ConflictFailure(message: e.toString()));
      }
      if (e.response?.statusCode == 204) {
        return Left(NotFoundFailure(message: e.toString()));
      }
      if (e.response?.statusCode == 404) {
        return Left(NotFoundFailure(message: e.toString()));
      }
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    } on Exception catch (e) {
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    }
  }

  @override
  ResultFuture<void> reserveSeats(String shoppingCartId) async {
    try {
      final response = await _client.post(
          '/api/shoppingcarts/${shoppingCartId}/reservations',
          options:
              Options(headers: {'X-Idempotency-Key': Guid.newGuid.toString()}));

      return Right(null);
    } on Exception catch (e) {
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    }
  }
}
