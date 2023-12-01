import 'package:equatable/equatable.dart';

class ShoppingCartSeat extends Equatable {
  final int? seatRow;
  final int? seatNumber;
  final DateTime? selectionExpirationTime;
  final double? price;

  const ShoppingCartSeat({this.seatRow, this.seatNumber, this.selectionExpirationTime, this.price});

  @override
  // TODO: implement props
  List<Object?> get props => [seatRow, seatNumber, selectionExpirationTime, price];
}

class PriceCalculationResult extends Equatable {
  final double? totalCartAmountBeforeDiscounts;
  final double? totalCartAmountAfterDiscounts;
  final double? totalCartDiscounts;

  const PriceCalculationResult({this.totalCartDiscounts, this.totalCartAmountBeforeDiscounts, this.totalCartAmountAfterDiscounts});

  @override
  // TODO: implement props
  List<Object?> get props => [totalCartDiscounts, totalCartAmountBeforeDiscounts, totalCartAmountAfterDiscounts];
}

// public record PriceCalculationResult (decimal TotalCartAmountBeforeDiscounts,
//     decimal TotalCartAmountAfterDiscounts,
//     decimal TotalCartDiscounts, ICollection<AppliedPriceRule> AppliedPriceRules);
