import 'package:equatable/equatable.dart';





class Seat extends Equatable {
  final int? seatRow;
  final int? seatNumber;

  const Seat({this.seatRow, this.seatNumber});

  @override
  // TODO: implement props
  List<Object?> get props => [seatRow, seatNumber];
}