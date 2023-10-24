import 'package:equatable/equatable.dart';

class Seat extends Equatable {
  final int seatNumber;
  final int row;
  final bool blocked;
  late bool isCurrentReserve;
  final String hashId;

  Seat(
      {required this.row,
      required this.seatNumber,
      required this.blocked,
      required this.hashId,
      this.isCurrentReserve = false});

  Seat.temp(
      {required this.row,
      required this.seatNumber,
      required this.blocked,
      required this.isCurrentReserve,
      required this.hashId});

  @override
  List<Object?> get props => [seatNumber, row, blocked, isCurrentReserve];
}
