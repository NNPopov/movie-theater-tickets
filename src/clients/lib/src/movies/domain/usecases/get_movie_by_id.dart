import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../entities/movie.dart';
import '../repos/movie_repo.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class GetMovieById
    extends FutureUsecaseWithParams<Movie, String> {

  GetMovieById({MovieRepo? repo})
  {
    _repo = repo ?? getIt.get<MovieRepo>();

  }

  late MovieRepo _repo;

  @override
  ResultFuture<Movie> call(String movieId) =>
      _repo.getMovieById(movieId);
}