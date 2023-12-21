import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../movie_sessions/domain/entities/active_movie.dart';
import '../../../movie_sessions/domain/repos/movie_session_repo.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class GetActiveMovies
    extends FutureUsecaseWithoutParams<List<ActiveMovie>> {

  GetActiveMovies(this._repo);
  final MovieSessionRepo _repo;

  @override
  ResultFuture<List<ActiveMovie>> call() =>
      _repo.getActiveMovies();
}