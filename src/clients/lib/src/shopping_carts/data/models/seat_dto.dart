import '../../domain/entities/seat.dart';

class ShoppingCartSeatDto extends ShoppingCartSeat {
  const ShoppingCartSeatDto(
      {super.seatRow, super.seatNumber, super.selectionExpirationTime});

  ShoppingCartSeatDto.fromJson(Map<String, dynamic> json)
      : super(
          seatRow: json['seatRow'],
          seatNumber: json['seatNumber'],
          selectionExpirationTime: json['selectionExpirationTime'] != null
              ? DateTime.parse(json['selectionExpirationTime'])
              : null,
        );

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {};
    data['seatRow'] = seatRow;
    data['seatNumber'] = seatNumber;
    data['selectionExpirationTime'] = selectionExpirationTime.toString();
    return data;
  }
}
