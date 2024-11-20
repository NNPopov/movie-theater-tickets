import 'dart:convert';
import '../../domain/entity/cinema_hall_info.dart';
import '../../domain/entity/cinema_seat.dart';
import 'cinema_seat_dto.dart';

class CinemaHallInfoDto extends CinemaHallInfo {
  const CinemaHallInfoDto(super.id, super.description, super.cinemaSeat);

  Map<String, dynamic> toMap() {
    return <String, dynamic>{
      'id': id,
      'description': description,
      'seat': cinemaSeat
    };
  }

  factory CinemaHallInfoDto.fromMap(Map<String, dynamic> map) {
    var seats = getSeats(map['seats'] as List<dynamic>);
    return CinemaHallInfoDto(
      map['id'] as String,
      map['description'] as String,
      seats,
    );
  }

  String toJson() => json.encode(toMap());

  factory CinemaHallInfoDto.fromJson(dynamic source) =>
      CinemaHallInfoDto.fromMap(source as Map<String, dynamic>);

  static List<List<CinemaSeat>> getSeats(List map) {
      var seats = List<dynamic>.from(map)
        .map((e) => List<Map<String, dynamic>>.from(e.toList())
            .map((t) =>
                CinemaSeatDto.fromJson(t as Map<String, dynamic>) as CinemaSeat)
            .toList())
        .toList();
    return seats;
  }
}
