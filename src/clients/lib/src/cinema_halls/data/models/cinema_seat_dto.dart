import 'dart:convert';
import '../../domain/entity/cinema_seat.dart';


class CinemaSeatDto extends CinemaSeat {
  const CinemaSeatDto(
  {required super.row, required super.seatNumber});

  Map<String, dynamic> toMap() {
    return <String, dynamic>{
      'row': row,
      'seatNumber': seatNumber
    };
  }

  factory CinemaSeatDto.fromMap(Map<String, dynamic> map) {
    return CinemaSeatDto(
      row: map['row'] ,
      seatNumber: map['seatNumber'] ,
    );
  }

  String toJson() => json.encode(toMap());

  factory CinemaSeatDto.fromJson(dynamic source) => CinemaSeatDto.fromMap(source as Map<String, dynamic>);
}
