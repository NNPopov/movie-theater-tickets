import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../entities/seat.dart';
import 'package:get_it/get_it.dart';

import '../repos/seat_repo.dart';

GetIt getIt = GetIt.instance;

class GetSeatsByMovieSessionId
    extends FutureUsecaseWithParams<List<Seat>, String> {

  GetSeatsByMovieSessionId({SeatRepo? repo})
  {
    _repo = repo ?? getIt.get<SeatRepo>();

  }

  late SeatRepo _repo;

  @override
  ResultFuture<List<Seat>> call(String movieSessionId) =>
      _repo.getSeatsByMovieSessionId(movieSessionId);
}