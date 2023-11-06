import '../../../../core/utils/typedefs.dart';
import '../entities/seat.dart';

abstract class SeatRepo {
  const SeatRepo();

  ResultFuture<List<Seat>> getSeatsByMovieSessionId(String movieSessionId);
}