import 'package:equatable/equatable.dart';

class ShoppingCartSeat extends Equatable {
  final int? seatRow;
  final int? seatNumber;

  const ShoppingCartSeat({this.seatRow, this.seatNumber});

  @override
  // TODO: implement props
  List<Object?> get props => [seatRow, seatNumber];
}