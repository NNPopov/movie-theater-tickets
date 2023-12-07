import 'dart:convert';
import '../../domain/entities/active_movie.dart';

class ActiveMovieDto extends ActiveMovie {
  const ActiveMovieDto(
      super.id, super.title,);

  Map<String, dynamic> toMap() {
    return <String, dynamic>{
      'id': id,
      'title': title,
    };
  }

  factory ActiveMovieDto.fromMap(Map<String, dynamic> map) {
    return ActiveMovieDto(
      map['id'] as String,
      map['title'] as String
    );
  }

  String toJson() => json.encode(toMap());

  factory ActiveMovieDto.fromJson(dynamic source) => ActiveMovieDto.fromMap(source as Map<String, dynamic>);
}
