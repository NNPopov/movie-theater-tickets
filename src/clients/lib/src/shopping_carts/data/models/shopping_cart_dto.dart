import '../../../../core/utils/typedefs.dart';
import '../../domain/entities/seat.dart';
import 'seat_dto.dart';
import '../../domain/entities/shopping_cart.dart';
import 'package:equatable/equatable.dart';


class ShoppingCartDto extends ShoppingCart {
   ShoppingCartDto(
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
                    .map( (e)=>ShoppingCartSeatDto.fromJson(e) as ShoppingCartSeat )
                    .toList());

   ShoppingCartDto.empty()
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
    data['seats'] = shoppingCartSeat != null
        ? shoppingCartSeat!.map((v) => (v as ShoppingCartSeatDto).toJson()).toList()
        : null;
    return data;
  }

  ShoppingCart copyWith({
    int? maxNumberOfSeats,
    DateTime? createdCard,
    String? id,
    String? movieSessionId,
    int? status,
    List<ShoppingCartSeatDto>? seats,
  }) {
    return ShoppingCart(
        maxNumberOfSeats: maxNumberOfSeats ?? this.maxNumberOfSeats,
        createdCard: createdCard ?? this.createdCard,
        id: id ?? this.id,
        movieSessionId: movieSessionId ?? this.movieSessionId,
        status: status ?? this.status,
        seats: seats ?? shoppingCartSeat);
  }


}

extension  ShoppingCarMap on ShoppingCart{

  ShoppingCartDto map(){
     return ShoppingCartDto( id:  this.id,
      maxNumberOfSeats:this.maxNumberOfSeats,
      createdCard:this.createdCard,

      movieSessionId:this.movieSessionId,
      status:this.status,
      seats:this.shoppingCartSeat
  );
  }
}
