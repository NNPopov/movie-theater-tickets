import 'package:equatable/equatable.dart';

class Seat extends Equatable {
  final int seatNumber;
  final int row;
  final bool blocked;
  late bool isCurrentReserve;
  final String hashId;
  final SeatStatus seatStatus;

  Seat(
      {required this.row,
      required this.seatNumber,
      required this.blocked,
      required this.hashId,
      required this.seatStatus,
      this.isCurrentReserve = false});

  Seat.temp({
    required this.row,
    required this.seatNumber,
    required this.blocked,
    required this.hashId,
    required this.seatStatus,
    required this.isCurrentReserve,
  });

  @override
  List<Object?> get props => [seatNumber, row, blocked, isCurrentReserve, seatStatus];
}

enum SeatStatus { blocked, available, selected, reserved, sold }
