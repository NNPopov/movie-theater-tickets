import 'package:equatable/equatable.dart';





class Seat extends Equatable {
  int? seatRow;
  int? seatNumber;

  Seat({this.seatRow, this.seatNumber});

  @override
  // TODO: implement props
  List<Object?> get props => [seatRow, seatNumber];
}