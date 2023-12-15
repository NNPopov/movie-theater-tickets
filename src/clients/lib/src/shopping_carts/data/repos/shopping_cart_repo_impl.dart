import 'dart:convert';
import 'package:dartz/dartz.dart';
import 'package:movie_theater_tickets/core/utils/typedefs.dart';
import 'package:dio/dio.dart';
import '../../../../core/errors/failures.dart';
import '../../../hub/domain/event_hub.dart';
import '../../domain/entities/create_shopping_cart_response.dart';
import '../../domain/entities/shopping_cart.dart';
import '../../domain/repos/shopping_cart_repo.dart';
import '../models/create_shopping_cart_dto.dart';
import '../models/seat_info_dto.dart';
import '../models/shopping_cart_dto.dart';
import 'package:flutter_guid/flutter_guid.dart';
import 'package:get_it/get_it.dart';
import 'package:dio_cache_interceptor/dio_cache_interceptor.dart';

GetIt getIt = GetIt.instance;

class ShoppingCartRepoImpl implements ShoppingCartRepo {
  final Dio _client;
  final EventHub _eventHub;

  ShoppingCartRepoImpl(this._client, this._eventHub);

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
          response.data as Map<String, dynamic>);

      return Right(shoppingCart);
    } on Exception catch (e) {
      return Left(
          ServerFailure(message: e.toString(), statusCode: e.toString()));
    }
  }

  @override
  ResultFuture<ShoppingCart> getShoppingCart(String shoppingCartId) async {
    try {
      const options = CacheOptions(
        policy: CachePolicy.noCache,
        store: null,
      );
     var extra = _client.options.extra[CacheResponse.cacheKey];

     // final cacheOptions = CacheOptions.fromExtra(extra)!;

      final response = await _client.get(
        '/api/shoppingcarts/$shoppingCartId',
        options: Options(
            extra: {
              CacheResponse.cacheKey:
              options.copyWith(policy: CachePolicy.noCache).toOptions(),
            })
      );

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
  ResultFuture<void> selectSeat(SeatInfoDto seatInfo) async {
    try {
      _eventHub.seatSelect(seatInfo);
      return const Right(null);
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
  ResultFuture<void> unselectSeat(SeatInfoDto seatInfo) async {
    try {
      _eventHub.seatUnselect(seatInfo);
      return const Right(null);
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
        return const Left(NotFoundFailure(statusCode: 204));
      }

      var shoppingCart = CreateShoppingCartResponse.fromJson(
          response.data as Map<String, dynamic>);

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
          '/api/shoppingcarts/$shoppingCartId/assignclient',
          options:
              Options(headers: {'X-Idempotency-Key': Guid.newGuid.toString()}));

      return const Right(null);
    } on DioException catch (e) {
      if (e.response?.statusCode == 401) {
        return Left(ConflictFailure(message: e.toString()));
      }
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
          '/api/shoppingcarts/$shoppingCartId/reservations',
          options:
              Options(headers: {'X-Idempotency-Key': Guid.newGuid.toString()}));

      return const Right(null);
    } on Exception catch (e) {
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    }
  }
}
