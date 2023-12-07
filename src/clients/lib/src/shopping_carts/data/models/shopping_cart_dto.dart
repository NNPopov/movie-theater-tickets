import 'package:movie_theater_tickets/src/shopping_carts/data/models/price_calculation_result_dto.dart';

import '../../domain/entities/price_calculation_result.dart';
import '../../domain/entities/seat.dart';
import 'seat_dto.dart';
import '../../domain/entities/shopping_cart.dart';

class ShoppingCartDto extends ShoppingCart {
  ShoppingCartDto(
      {super.maxNumberOfSeats,
      super.createdAt,
      super.id,
      super.movieSessionId,
      super.status,
      super.seats,
      super.isAssigned,
      super.priceCalculationResult});

  ShoppingCartDto.fromJson(Map<String, dynamic> json)
      : super(
          maxNumberOfSeats: json['maxNumberOfSeats'],
          createdAt: DateTime.parse(json['createdAt']),
          id: json['id'],
          movieSessionId: json['movieSessionId'],
          status: ShoppingCartStatus.values[json['status']],
          seats: List<Map<String, dynamic>>.from(json['seats'] as List<dynamic>)
              .map((e) =>
                  ShoppingCartSeatDto.fromJson(e as Map<String, dynamic>)
                      as ShoppingCartSeat)
              .toList(),
          isAssigned: json['isAssigned'] ?? false,
          priceCalculationResult: json['priceCalculationResult'] != null
              ? PriceCalculationResultDto.fromJson(
                      json['priceCalculationResult'] as Map<String, dynamic>)
                  as PriceCalculationResult
              : null,
        );

  ShoppingCartDto.empty()
      : this(
            maxNumberOfSeats: 0,
            createdAt: null,
            id: '',
            movieSessionId: '',
            status: ShoppingCartStatus.InWork,
            seats: null,
            isAssigned: false,
            priceCalculationResult: null);

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {};
    data['maxNumberOfSeats'] = maxNumberOfSeats;
    data['createdAt'] = createdAt.toString();
    data['id'] = id;
    data['movieSessionId'] = movieSessionId;
    data['status'] = status?.index;
    data['seats'] = shoppingCartSeat != null
        ? shoppingCartSeat!
            .map((v) => (v as ShoppingCartSeatDto).toJson())
            .toList()
        : null;
    data['isAssigned'] = isAssigned;
    data['priceCalculationResult'] = priceCalculationResult != null
        ? (priceCalculationResult as PriceCalculationResultDto).toJson()
        : null;

    return data;
  }

  ShoppingCart copyWith(
      {int? maxNumberOfSeats,
      DateTime? createdAt,
      String? id,
      String? movieSessionId,
      ShoppingCartStatus? status,
      List<ShoppingCartSeatDto>? seats,
      bool? isAssigned,
      PriceCalculationResult? priceCalculationResult}) {
    return ShoppingCart(
        maxNumberOfSeats: maxNumberOfSeats ?? this.maxNumberOfSeats,
        createdAt: createdAt ?? this.createdAt,
        id: id ?? this.id,
        movieSessionId: movieSessionId ?? this.movieSessionId,
        status: status ?? this.status,
        seats: seats ?? shoppingCartSeat,
        isAssigned: isAssigned ?? this.isAssigned,
        priceCalculationResult:
            priceCalculationResult ?? this.priceCalculationResult);
  }
}

extension ShoppingCarMap on ShoppingCart {
  ShoppingCartDto map() {
    return ShoppingCartDto(
      id: this.id,
      maxNumberOfSeats: this.maxNumberOfSeats,
      createdAt: this.createdAt,
      movieSessionId: this.movieSessionId,
      status: this.status,
      seats: this.shoppingCartSeat,
      isAssigned: this.isAssigned,
      priceCalculationResult: this.priceCalculationResult,
    );
  }
}
