import 'package:equatable/equatable.dart';

class CinemaSeat extends Equatable {
  final int seatNumber;
  final int row;


  const CinemaSeat(
      {required this.row,
        required this.seatNumber});

  const CinemaSeat.temp({
    required this.row,
    required this.seatNumber
  });

  @override
  List<Object?> get props => [seatNumber, row];
}
