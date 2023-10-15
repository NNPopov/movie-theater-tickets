import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../entity/cinema_hall.dart';
import '../repo/cinema_hall_repo.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class GetCinemaHallById
    extends FutureUsecaseWithParams<CinemaHall, String> {

  GetCinemaHallById({CinemaHallRepo? repo})
  {
    _repo = repo ?? getIt.get<CinemaHallRepo>();

  }

  late CinemaHallRepo _repo;

  @override
  ResultFuture<CinemaHall> call(String cinemaHallId) =>
      _repo.getCinemaHallById(cinemaHallId);
}