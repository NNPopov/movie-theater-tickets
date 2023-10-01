import '../../../../core/utils/typedefs.dart';
import 'seat_dto.dart';
import '../../domain/entities/shopping_cart.dart';

class ShoppingCartDto extends ShoppingCart {
  const ShoppingCartDto(
      {super.maxNumberOfSeats,
      super.createdCard,
      super.id,
      super.movieSessionId,
      super.status,
      super.seats});

  ShoppingCartDto.fromJson(Map<String, dynamic> json)
      : super(
            maxNumberOfSeats: json['maxNumberOfSeats'],
            createdCard: DateTime.parse(json['createdCard']),
            id: json['id'],
            movieSessionId: json['movieSessionId'],
            status: json['status'],
            seats:
                List<Map<String, dynamic>>.from(json['seats'] as List<dynamic>)
                    .map(SeatDto.fromJson)
                    .toList());

  const ShoppingCartDto.empty()
      : this(
          maxNumberOfSeats: 0,
          createdCard: null,
          id: '',
          movieSessionId: '',
          status: 0,
          seats: null,
        );

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {};
    data['maxNumberOfSeats'] = maxNumberOfSeats;
    data['createdCard'] = createdCard.toString();
    data['id'] = id;
    data['movieSessionId'] = movieSessionId;
    data['status'] = status;
    data['seats'] = seats != null
        ? seats!.map((v) => (v as SeatDto).toJson()).toList()
        : null;
    return data;
  }

  ShoppingCartDto copyWith({
    int? maxNumberOfSeats,
    DateTime? createdCard,
    String? id,
    String? movieSessionId,
    int? status,
    List<SeatDto>? seats,
  }) {
    return ShoppingCartDto(
        maxNumberOfSeats: maxNumberOfSeats ?? this.maxNumberOfSeats,
        createdCard: createdCard ?? this.createdCard,
        id: id ?? this.id,
        movieSessionId: movieSessionId ?? this.movieSessionId,
        status: status ?? this.status,
        seats: seats ?? this.seats);
  }
}
