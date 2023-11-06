import 'dart:convert';

import '../../domain/entity/cinema_hall.dart';


class CinemaHallDto extends CinemaHall {
  const CinemaHallDto(
      super.id, super.description);

  Map<String, dynamic> toMap() {
    return <String, dynamic>{
      'id': id,
      'description': description
    };
  }

  factory CinemaHallDto.fromMap(Map<String, dynamic> map) {
    return CinemaHallDto(
      map['id'] as String,
      map['description'] as String,
    );
  }

  String toJson() => json.encode(toMap());

  factory CinemaHallDto.fromJson(dynamic source) => CinemaHallDto.fromMap(source as Map<String, dynamic>);
}
