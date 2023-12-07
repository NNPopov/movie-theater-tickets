import '../../../../core/utils/typedefs.dart';
import '../entity/cinema_hall.dart';
import '../entity/cinema_hall_info.dart';

abstract class CinemaHallRepo {
  const CinemaHallRepo();

  ResultFuture<CinemaHall> getCinemaHallById(String cinemaHallId);

  ResultFuture<CinemaHallInfo> getCinemaHallInfoById(String cinemaHallId);
}