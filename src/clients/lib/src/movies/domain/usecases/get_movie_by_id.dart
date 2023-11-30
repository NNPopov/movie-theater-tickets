import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../entities/movie.dart';
import '../repos/movie_repo.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class GetMovieById
    extends FutureUsecaseWithParams<Movie, String> {

  GetMovieById(this._repo);
  final MovieRepo _repo;

  @override
  ResultFuture<Movie> call(String movieId) =>
      _repo.getMovieById(movieId);
}