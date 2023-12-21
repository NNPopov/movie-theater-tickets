import 'dart:convert';
import 'package:dartz/dartz.dart';
import 'package:movie_theater_tickets/core/utils/typedefs.dart';
import 'package:dio/dio.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/entities/active_movie.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/entities/movie_session.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/repos/movie_session_repo.dart';
import '../models/active_movie_dto.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class MovieSessionRepoImpl implements MovieSessionRepo {
  final Dio _client;

  MovieSessionRepoImpl(this._client);

  @override
  ResultFuture<MovieSession> getMovieSession(String movieSessionId) async {
    try {
      final movieSessionResponse  = await _client.get('/api/moviesessions/$movieSessionId');
      var movieSessionData  = json.decode(movieSessionResponse .toString());

      var movieSessionDto  = MovieSession.fromJson(movieSessionData );

      return Right(movieSessionDto );
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message, statusCode: e.statusCode));
    }
  }

  @override
  ResultFuture<List<MovieSession>> getMovieSessionByMovieId(
      String movieId) async {
    try {
      final movieSessionsResponse =
          await _client.get('/api/movies/$movieId/moviesessions');
      var movieSessionsData = movieSessionsResponse.data as List;

      var movieSessionList = List<MovieSession>.from(
          movieSessionsData.map((model) => MovieSession.fromJson(model)));

      return Right(movieSessionList);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message, statusCode: e.statusCode));
    }
  }

  @override
  ResultFuture<List<ActiveMovie>> getActiveMovies() async {
    try {
      final activeMoviesResponse =
          await _client.get('/api/moviesessions/activemovies');
      var activeMoviesData = activeMoviesResponse.data as List;

      var activeMovieList = List<ActiveMovieDto>.from(
          activeMoviesData.map((model) => ActiveMovieDto.fromJson(model)));

      return Right(activeMovieList);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message, statusCode: e.statusCode));
    }
  }
}
