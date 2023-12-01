import '../../domain/entities/seat.dart';

class ShoppingCartSeatDto extends ShoppingCartSeat {
  const ShoppingCartSeatDto(
      {super.seatRow, super.seatNumber, super.selectionExpirationTime, super.price});

  ShoppingCartSeatDto.fromJson(Map<String, dynamic> json)
      : super(
          seatRow: json['seatRow'],
          seatNumber: json['seatNumber'],
          selectionExpirationTime: json['selectionExpirationTime'] != null
              ? DateTime.parse(json['selectionExpirationTime'])
              : null,
          price: json['price'],
        );

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {};
    data['seatRow'] = seatRow;
    data['seatNumber'] = seatNumber;
    data['selectionExpirationTime'] = selectionExpirationTime.toString();
    data['price'] = price;
    return data;
  }
}

class PriceCalculationResultDto extends PriceCalculationResult {
  const PriceCalculationResultDto(
      {super.totalCartDiscounts, super.totalCartAmountBeforeDiscounts, super.totalCartAmountAfterDiscounts});

  PriceCalculationResultDto.fromJson(Map<String, dynamic> json)
      : super(
    totalCartDiscounts: json['totalCartDiscounts'],
    totalCartAmountBeforeDiscounts: json['totalCartAmountBeforeDiscounts'] ,
    totalCartAmountAfterDiscounts: json['totalCartAmountAfterDiscounts'],
  );

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {};
    data['totalCartDiscounts'] = totalCartDiscounts;
    data['totalCartAmountBeforeDiscounts'] = totalCartAmountBeforeDiscounts;
    data['totalCartAmountAfterDiscounts'] = totalCartAmountAfterDiscounts;
    return data;
  }
}
