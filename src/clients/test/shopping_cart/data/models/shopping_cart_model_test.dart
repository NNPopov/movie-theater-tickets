import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:movie_theater_tickets/src/shopping_carts/data/models/seat_dto.dart';
import 'package:movie_theater_tickets/src/shopping_carts/data/models/shopping_cart_dto.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/entities/shopping_cart.dart';

import '../../../fixtures/fixture_reader.dart';

void main() {
  final tUserExamModel = ShoppingCartDto.empty();

  final tShoppingCarDto = ShoppingCartDto(
      id: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      createdCard: DateTime.parse("2023-09-28 19:28:53.299Z"),
      movieSessionId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      maxNumberOfSeats: 4,
      status: 1,
      seats: [
        SeatDto(seatNumber: 1, seatRow: 1),
        SeatDto(seatNumber: 2, seatRow: 1),
        SeatDto(seatNumber: 3, seatRow: 1)
      ]);

  group('UserExamModel', () {
    test('should be a subclass of [ShoppingCart] entity', () async {
      expect(tUserExamModel, isA<ShoppingCart>());
    });

    group('fromMap', () {
      test('should return a valid [ShoppingCartDto] when the JSON is not null',
          () async {
        final map = jsonDecode(fixture('shopping_cart.json')) as Map<String, dynamic> ;
        final result = ShoppingCartDto.fromJson(map);
        expect(result, tShoppingCarDto);
      });
    });

    group('toMap', () {
      test('should return a Dart map containing the proper data', () async {
        final map = jsonDecode(fixture('shopping_cart.json')) as Map<String, dynamic> ;
        final result = tShoppingCarDto.toJson();
        expect(result, map);
      });
    });
    //
    // group('copyWith', () {
    //   test('should return a new [UserExamModel] with the same values',
    //           () async {
    //         final result = tUserExamModel.copyWith(examId: '');
    //         expect(result.examId, equals(''));
    //       });
    // });
  });
}
