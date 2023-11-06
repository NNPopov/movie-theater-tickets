import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../entities/movie_session.dart';
import 'package:get_it/get_it.dart';

import '../repos/movie_session_repo.dart';

GetIt getIt = GetIt.instance;

class GetMovieSessions
    extends FutureUsecaseWithParams<List<MovieSession>, String> {

  GetMovieSessions({MovieSessionRepo? repo})
  {
    _repo = repo ?? getIt.get<MovieSessionRepo>();

  }

  late MovieSessionRepo _repo;

  @override
  ResultFuture<List<MovieSession>> call(String movieId) =>
      _repo.getMovieSessionByMovieId(movieId);
}