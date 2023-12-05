import 'dart:convert';
import 'package:equatable/equatable.dart';

class SeatInfoDto extends Equatable {
  final int row;
  final int number;
  final String showtimeId;
  final String shoppingCartId;

  const SeatInfoDto(
      {required this.row,
        required this.number,
        required this.showtimeId,
        required this.shoppingCartId});

  @override
  List<Object> get props => [row, number, showtimeId];

  Map<String, dynamic> toMap() {
    return <String, dynamic>{
      'row': row,
      'number': number,
      'showtimeId': showtimeId,
      'shoppingCartId': shoppingCartId
    };
  }

  String toJson() => json.encode(toMap());

  @override
  bool get stringify => true;
}
