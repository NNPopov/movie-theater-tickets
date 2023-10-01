import 'dart:convert';
import 'package:dartz/dartz.dart';
import 'package:movie_theater_tickets/core/utils/typedefs.dart';
import 'package:dio/dio.dart';
import 'package:movie_theater_tickets/src/movies/domain/entities/movie.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/repos/movie_repo.dart';
import '../models/movie_dto.dart';

class MovieRepoImpl extends MovieRepo {
  final Dio _client;

  MovieRepoImpl(this._client);

  @override
  ResultFuture<List<Movie>> getMovie() async {
    try {


      final response = await _client.get('/api/movies');
      var movies = response.data as List;

      var movieDtos = List<MovieDto>.from(
          movies.map((model) => MovieDto.fromJson(model)));

      return Right(movieDtos);
    } on DioException catch (e) {
      return Left(ServerFailure(message: json.decode(e.response.toString())["errorMessage"], statusCode: e.message));
    }
  }

  @override
  ResultFuture<Movie> getMovieById(String movieId) async {
    try {
      final response = await _client.get('/api/movies/$movieId');
      var movieSession = json.decode(response.toString());

      var movieSessionDto = MovieDto.fromJson(movieSession);

      return Right(movieSessionDto);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message, statusCode: e.statusCode));
    }
  }
}
