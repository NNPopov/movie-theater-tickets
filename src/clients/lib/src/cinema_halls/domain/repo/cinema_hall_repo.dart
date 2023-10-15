import '../../../../core/utils/typedefs.dart';
import '../entity/cinema_hall.dart';

abstract class CinemaHallRepo {
  const CinemaHallRepo();

  ResultFuture<CinemaHall> getCinemaHallById(String cinemaHallId);
}