import '../../domain/entities/price_calculation_result.dart';

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