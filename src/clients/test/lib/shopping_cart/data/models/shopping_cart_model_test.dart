import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:movie_theater_tickets/src/shopping_carts/data/models/seat_dto.dart';
import 'package:movie_theater_tickets/src/shopping_carts/data/models/shopping_cart_dto.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/entities/shopping_cart.dart';

import '../../../../fixtures/fixture_reader.dart';

void main() {
  final shoppingCartDtoEmpty = ShoppingCartDto.empty();

  final tShoppingCarDto = ShoppingCartDto(
      id: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      createdAt: DateTime.parse("2023-09-28 19:28:53.299Z"),
      movieSessionId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      maxNumberOfSeats: 4,
      status: ShoppingCartStatus.SeatsReserved,
      isAssigned: true,
      seats: [
        ShoppingCartSeatDto(
            seatNumber: 1,
            seatRow: 1,
            selectionExpirationTime: DateTime.parse("2023-09-28 19:28:53.299Z"),
            price: 15.0,
            isDirty: false),
        ShoppingCartSeatDto(
            seatNumber: 2,
            seatRow: 1,
            selectionExpirationTime: DateTime.parse("2023-09-28 19:28:53.299Z"),
            price: 15.0,
            isDirty: false),
        ShoppingCartSeatDto(
            seatNumber: 3,
            seatRow: 1,
            selectionExpirationTime: DateTime.parse("2023-09-28 19:28:53.299Z"),
            price: 15.0,
            isDirty: false),
      ],
      isDirty: false);

  group('ShoppingCart', () {
    test('should be a subclass of [ShoppingCart] entity', () async {
      expect(shoppingCartDtoEmpty, isA<ShoppingCart>());
    });
  });

  test('should return a valid [ShoppingCartDto] when the JSON is not null',
      () async {
    final map =
        jsonDecode(fixture('shopping_cart.json')) as Map<String, dynamic>;
    final result = ShoppingCartDto.fromJson(map);
    expect(result, tShoppingCarDto);
  });

  test('should return a Dart map containing the proper data', () async {
    final map =
        jsonDecode(fixture('shopping_cart.json')) as Map<String, dynamic>;
    final result = tShoppingCarDto.toJson();
    expect(result, map);
  });
  //
  // group('copyWith', () {
  //   test('should return a new [UserExamModel] with the same values',
  //           () async {
  //         final result = tUserExamModel.copyWith(examId: '');
  //         expect(result.examId, equals(''));
  //       });
  // });
}
