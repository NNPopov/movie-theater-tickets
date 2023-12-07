import 'package:equatable/equatable.dart';

class ShoppingCartSeat extends Equatable {
  final int? seatRow;
  final int? seatNumber;
  final DateTime? selectionExpirationTime;
  final double? price;
  late bool? isDirty;

   ShoppingCartSeat({this.seatRow, this.seatNumber, this.selectionExpirationTime, this.price, this.isDirty});

  @override
  // TODO: implement props
  List<Object?> get props => [seatRow, seatNumber, selectionExpirationTime, price, this.isDirty];
}


