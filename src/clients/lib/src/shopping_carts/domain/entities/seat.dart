import 'package:equatable/equatable.dart';

class ShoppingCartSeat extends Equatable {
  final int? seatRow;
  final int? seatNumber;
  final DateTime? selectionExpirationTime;

  const ShoppingCartSeat({this.seatRow, this.seatNumber, this.selectionExpirationTime});

  @override
  // TODO: implement props
  List<Object?> get props => [seatRow, seatNumber, selectionExpirationTime];
}