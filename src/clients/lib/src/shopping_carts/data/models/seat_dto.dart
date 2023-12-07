import '../../domain/entities/seat.dart';

class ShoppingCartSeatDto extends ShoppingCartSeat {
   ShoppingCartSeatDto(
      {super.seatRow, super.seatNumber, super.selectionExpirationTime, super.price, super.isDirty});

  ShoppingCartSeatDto.fromJson(Map<String, dynamic> json)
      : super(
          seatRow: json['seatRow'],
          seatNumber: json['seatNumber'],
          selectionExpirationTime: json['selectionExpirationTime'] != null
              ? DateTime.parse(json['selectionExpirationTime'])
              : null,
          price: json['price'],
          isDirty: json['isDirty'] ?? false,
        );

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {};
    data['seatRow'] = seatRow;
    data['seatNumber'] = seatNumber;
    data['selectionExpirationTime'] = selectionExpirationTime.toString();
    data['price'] = price;
    data['isDirty'] = isDirty;
    return data;
  }
}


