import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../entity/cinema_hall.dart';
import '../repo/cinema_hall_repo.dart';
import 'package:get_it/get_it.dart';

GetIt getIt = GetIt.instance;

class GetCinemaHallById
    extends FutureUsecaseWithParams<CinemaHall, String> {

  GetCinemaHallById(this._repo);

  final CinemaHallRepo _repo;

  @override
  ResultFuture<CinemaHall> call(String cinemaHallId) =>
      _repo.getCinemaHallById(cinemaHallId);
}