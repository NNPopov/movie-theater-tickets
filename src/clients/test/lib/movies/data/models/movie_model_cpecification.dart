import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:movie_theater_tickets/src/movies/domain/entities/movie.dart';
import '../../../../fixtures/fixture_reader.dart';

void main() {
  final tMovieDto = Movie(
      id: "e1fde23c-e26d-44d2-88f8-202951255001",
      title: "Inception",
      imdbId: "tt1375666",
      stars:
          "Leonardo DiCaprio, Joseph Gordon-Levitt, Ellen Page, Ken Watanabe",
      releaseDate: DateTime.parse("2010-01-14 00:00:00.000Z"));

  group('Movie', () {
    test('should be a subclass of [Movie]', () async {
      expect(tMovieDto, isA<Movie>());
    });

    test('should return a valid [movie DTO] when the JSON is not null',
        () async {
      final map = jsonDecode(fixture('movie.json')) as Map<String, dynamic>;
      final result = Movie.fromJson(map);
      expect(result, tMovieDto);
    });
  });
}
