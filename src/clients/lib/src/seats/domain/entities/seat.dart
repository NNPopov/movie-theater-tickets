import 'package:equatable/equatable.dart';
class Seat extends Equatable
{

  final int seatNumber;
  final int row;
  final bool blocked;
  late bool initBlocked;

   Seat({required this.row, required this.seatNumber,  required this.blocked})
  {
    initBlocked = this.blocked;
  }

  Seat.temp({required this.row, required this.seatNumber,  required this.blocked,required this.initBlocked});

  @override

  List<Object?> get props => [seatNumber, row, blocked];
}