import '../../domain/entities/seat.dart';

class SeatDto extends Seat {

  SeatDto({super.seatRow, super.seatNumber});

  SeatDto.fromJson(Map<String, dynamic> json):super(
    seatRow : json['seatRow'],
    seatNumber : json['seatNumber'],
    );

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {};
    data['seatRow'] = seatRow;
    data['seatNumber'] = seatNumber;
    return data;
  }
}