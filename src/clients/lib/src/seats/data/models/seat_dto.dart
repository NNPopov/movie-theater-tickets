import '../../domain/entities/seat.dart';

class SeatDto extends Seat {

  SeatDto({required super.row, required super.seatNumber, required super.blocked, required super.hashId});

  SeatDto.fromJson(Map<String, dynamic> json):super(
    row : json['row'],
    seatNumber : json['seatNumber'],
    blocked : json['blocked'],
      hashId : json['hashId']
    );

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {};
    data['row'] = row;
    data['seatNumber'] = seatNumber;
    data['blocked'] = blocked;
    data['hashId'] = hashId;
    return data;
  }
}