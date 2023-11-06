import '../../../../core/utils/typedefs.dart';
import '../entities/movie.dart';

abstract class MovieRepo {
  const MovieRepo();

  ResultFuture<Movie> getMovieById(String movieId);

  ResultFuture<List<Movie>> getMovies();
}