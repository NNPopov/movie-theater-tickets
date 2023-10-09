import '../../domain/entities/seat.dart';

class SeatDto extends Seat {

  SeatDto({required super.row, required super.seatNumber, required super.blocked});

  SeatDto.fromJson(Map<String, dynamic> json):super(
    row : json['row'],
    seatNumber : json['seatNumber'],
    blocked : json['blocked'],
    );

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {};
    data['row'] = row;
    data['seatNumber'] = seatNumber;
    data['blocked'] = blocked;
    return data;
  }
}