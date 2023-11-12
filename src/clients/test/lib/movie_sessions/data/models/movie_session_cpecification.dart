import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:movie_theater_tickets/src/movie_sessions/data/models/movie_session_dto.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/entities/movie_session.dart';
import 'package:movie_theater_tickets/src/movies/data/models/movie_dto.dart';
import 'package:movie_theater_tickets/src/movies/domain/entities/movie.dart';
import '../../../../fixtures/fixture_reader.dart';

void main() {
  final movieSessionDto = MovieSessionDto(
       "97207ae2-e5dd-4084-903a-5655966cd101",
       "e1fde23c-e26d-44d2-88f8-202951255001",
       DateTime.parse("2023-11-20T00:00:00+00:00"),
       "97207ae2-e5dd-4084-903a-5655966ca010");

  group('MovieSession', () {
    test('should be a subclass of [Movie]', () async {
      expect(movieSessionDto, isA<MovieSession>());
    });
  });

  group('MovieSessionFromMap', () {
    test('should return a valid [movie DTO] when the JSON is not null',
        () async {
      final map =
          jsonDecode(fixture('movie_session.json')) as Map<String, dynamic>;
      final result = MovieSessionDto.fromJson(map);
      expect(result, movieSessionDto);
    });
  });
}
