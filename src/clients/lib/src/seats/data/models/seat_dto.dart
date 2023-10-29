import '../../domain/entities/seat.dart';

class SeatDto extends Seat {
  SeatDto(
      {required super.row,
      required super.seatNumber,
      required super.blocked,
      required super.hashId,
      required super.seatStatus});

  SeatDto.fromJson(Map<String, dynamic> json)
      : super(
          row: json['row'],
          seatNumber: json['seatNumber'],
          blocked: json['blocked'],
          hashId: json['hashId'],
          seatStatus: SeatStatus.values[json['seatStatus']],
        );

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = {};
    data['row'] = row;
    data['seatNumber'] = seatNumber;
    data['blocked'] = blocked;
    data['hashId'] = hashId;
    data['seatStatus'] = seatStatus.index;
    return data;
  }
}
