import 'package:freezed_annotation/freezed_annotation.dart';
import 'package:flutter/foundation.dart';

part 'movie_session.freezed.dart';

part 'movie_session.g.dart';

@freezed
class MovieSession with _$MovieSession {
  // final String id;
  // final String movieId;
  // final DateTime sessionDate;
  // final String cinemaHallId;

  const factory  MovieSession({required  String id,
    required  String movieId,
    required DateTime sessionDate,
    required  String cinemaHallId}) = _MovieSession;

  factory MovieSession.fromJson(Map<String, Object?> json) => _$MovieSessionFromJson(json);
  // @override
  // List<Object?> get props => [id];
}


// factory Movie(
// {required String id,
// required String title,
// String? imdbId,
// String? stars,
// DateTime? releaseDate}) = _Movie;
//
// factory Movie.fromJson(Map<String, Object?> json) => _$MovieFromJson(json);
//
// factory Movie.empty() {
// return _Movie(
// id: '',
// title: '',
// imdbId: '',
// stars: '',
// releaseDate: DateTime.parse('1900-01-01'),
// );