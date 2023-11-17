import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../entities/movie.dart';
import '../repos/movie_repo.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class GetMovies
    extends FutureUsecaseWithoutParams<List<Movie>> {

  GetMovies(this._repo);
  final MovieRepo _repo;

  @override
  ResultFuture<List<Movie>> call() =>
      _repo.getMovies();
}