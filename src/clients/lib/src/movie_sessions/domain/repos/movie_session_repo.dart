import '../../../../core/utils/typedefs.dart';
import '../entities/movie_session.dart';

abstract class MovieSessionRepo {
  const MovieSessionRepo();

  ResultFuture<List<MovieSession>> getMovieSessionByMovieId(String movieId);

  ResultFuture<MovieSession> getMovieSession(String shoppingCartId);
}