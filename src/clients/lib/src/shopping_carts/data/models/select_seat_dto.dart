import 'dart:convert';
import 'package:equatable/equatable.dart';

class SelectSeatShoppingCartDto extends Equatable {
  final int row;
  final int number;
  final String showtimeId;

  const SelectSeatShoppingCartDto(
      {required this.row, required this.number, required this.showtimeId});

  @override
  List<Object> get props => [row, number, showtimeId];

  Map<String, dynamic> toMap() {
    return <String, dynamic>{
      'row': row,
      'number': number,
      'showtimeId': showtimeId
    };
  }

  String toJson() => json.encode(toMap());

  @override
  bool get stringify => true;
}
