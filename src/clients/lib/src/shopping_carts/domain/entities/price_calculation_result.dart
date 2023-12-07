import 'package:equatable/equatable.dart';

class PriceCalculationResult extends Equatable {
  final double? totalCartAmountBeforeDiscounts;
  final double? totalCartAmountAfterDiscounts;
  final double? totalCartDiscounts;

  const PriceCalculationResult({this.totalCartDiscounts, this.totalCartAmountBeforeDiscounts, this.totalCartAmountAfterDiscounts});

  @override
  // TODO: implement props
  List<Object?> get props => [totalCartDiscounts, totalCartAmountBeforeDiscounts, totalCartAmountAfterDiscounts];
}
