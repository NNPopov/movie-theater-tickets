import 'dart:convert';
import 'package:dartz/dartz.dart';
import 'package:movie_theater_tickets/core/utils/typedefs.dart';
import 'package:dio/dio.dart';
import 'package:movie_theater_tickets/src/movie_sessions/domain/entities/movie_session.dart';
import '../../../../core/errors/exceptions.dart';
import '../../../../core/errors/failures.dart';
import '../../domain/repos/movie_session_repo.dart';
import '../models/movie_session_dto.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class MovieSessionRepoImpl implements MovieSessionRepo {
  final Dio _client;

  MovieSessionRepoImpl( this._client) ;

  @override
  ResultFuture<MovieSession> getMovieSession(String movieSessionId) async {
    try {
      final response = await _client.get('/api/moviesessions/$movieSessionId');
      var movieSession = json.decode(response.toString());

      var movieSessionDto = MovieSessionDto.fromJson(movieSession);

      return Right(movieSessionDto);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message, statusCode: e.statusCode));
    }
  }

  @override
  ResultFuture<List<MovieSession>> getMovieSessionByMovieId(
      String movieId) async {
    try {
      final response = await _client.get('/api/movies/$movieId/moviesessions');
      var movieSessions = response.data as List;

      var movieSessionDtos = List<MovieSessionDto>.from(
          movieSessions.map((model) => MovieSessionDto.fromJson(model)));

      return Right(movieSessionDtos);
    } on ServerException catch (e) {
      return Left(ServerFailure(message: e.message, statusCode: e.statusCode));
    }
  }
}
