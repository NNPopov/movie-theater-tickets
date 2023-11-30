import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../entities/movie_session.dart';
import 'package:get_it/get_it.dart';
import 'package:collection/collection.dart';
import 'package:dartz/dartz.dart';

import '../repos/movie_session_repo.dart';

GetIt getIt = GetIt.instance;

class GetMovieSessions
    extends FutureUsecaseWithParams<List<List<List<MovieSession>>>, String> {
  GetMovieSessions(this._repo);

  final MovieSessionRepo _repo;

  @override
  ResultFuture<List<List<List<MovieSession>>>> call(String movieId) async {
    var movieSessionsResponse = await _repo.getMovieSessionByMovieId(movieId);

    return movieSessionsResponse.fold((l) => Left(l), (movieSessions) {
      final movieSessionResult = groupBy(
              movieSessions,
              (movieSession) =>
                  '${movieSession.sessionDate.year}${movieSession.sessionDate.month}${movieSession.sessionDate.day}')
          .values
          .map((seatsByDate) => groupBy(
                  seatsByDate.toList(), (seatByDate) => seatByDate.cinemaHallId)
              .values
              .map((seatsBycinemaHallId) => seatsBycinemaHallId.toList()
                ..sort((a, b) => a.sessionDate.compareTo(b.sessionDate)))
              .toList()
            ..sort((a, b) => -a[0].cinemaHallId.compareTo(b[0].cinemaHallId)))
          .toList()
        ..sort((a, b) => a[0][0].sessionDate.compareTo(b[0][0].sessionDate));

      return Right(movieSessionResult);
    });

    //return movieSessionResult;
  }
}
