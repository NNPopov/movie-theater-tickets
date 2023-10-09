import 'package:equatable/equatable.dart';
class Seat extends Equatable
{

  final int seatNumber;
  final int row;
  final bool blocked;

  const Seat({required this.row, required this.seatNumber,  required this.blocked});

  @override

  List<Object?> get props => [seatNumber, row];
}