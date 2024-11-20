import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../cinema_halls/domain/entity/cinema_hall_info.dart';
import '../../../cinema_halls/domain/repo/cinema_hall_repo.dart';
import '../../../hub/domain/event_hub.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';


class GetCinemaHallInfo
    extends FutureUsecaseWithParams<CinemaHallInfo, String> {
  GetCinemaHallInfo(this._repo, this._eventHub);

  final CinemaHallRepo _repo;
  final EventHub _eventHub;
  final storage = const FlutterSecureStorage();

  @override
  ResultFuture<CinemaHallInfo> call(String params) async {
    var result = await _repo.getCinemaHallInfoById(params);

    //await _eventHub.seatsUpdateSubscribe(params);

    return result;
  }
}
