import 'dart:convert';

import '../../domain/entities/movie.dart';

class MovieDto extends Movie {
  const MovieDto(
      super.id, super.title, super.imdbId, super.stars, super.releaseDate);

  Map<String, dynamic> toMap() {
    return <String, dynamic>{
      'id': id,
      'title': title,
      'imdbId': imdbId,
      'stars': stars,
      'releaseDate': releaseDate.millisecondsSinceEpoch,
    };
  }

  factory MovieDto.fromMap(Map<String, dynamic> map) {
    return MovieDto(
      map['id'] as String,
      map['title'] as String,
      map['imdbId'] as String,
      map['stars'] as String,
      DateTime.parse(map['releaseDate']),
    );
  }

  String toJson() => json.encode(toMap());

  factory MovieDto.fromJson(dynamic source) => MovieDto.fromMap(source as Map<String, dynamic>);

  //factory MovieDto.fromJson(String source) => MovieDto.fromMap(json.decode(source) as Map<String, dynamic>);

}
