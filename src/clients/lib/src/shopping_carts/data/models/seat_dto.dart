import '../../domain/entities/seat.dart';

class ShoppingCartSeatDto extends ShoppingCartSeat {

  const ShoppingCartSeatDto({super.seatRow, super.seatNumber});

  ShoppingCartSeatDto.fromJson(Map<String, dynamic> json):super(
    seatRow : json['seatRow'],
    seatNumber : json['seatNumber'],
    );

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {};
    data['seatRow'] = seatRow;
    data['seatNumber'] = seatNumber;
    return data;
  }
}