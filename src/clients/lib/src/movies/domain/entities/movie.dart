import 'package:freezed_annotation/freezed_annotation.dart';
import 'package:flutter/foundation.dart';

part 'movie.freezed.dart';

part 'movie.g.dart';

@freezed
class Movie with _$Movie {
  factory Movie(
      {required String id,
      required String title,
      String? imdbId,
      String? stars,
      DateTime? releaseDate}) = _Movie;

  factory Movie.fromJson(Map<String, Object?> json) => _$MovieFromJson(json);

  factory Movie.empty() {
    return _Movie(
      id: '',
      title: '',
      imdbId: '',
      stars: '',
      releaseDate: DateTime.parse('1900-01-01'),
    );
  }
}
