import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../hub/domain/event_hub.dart';
import '../entities/seat.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:dartz/dartz.dart';
import '../repos/seat_repo.dart';

GetIt getIt = GetIt.instance;

class GetSeatsByMovieSessionId
    extends FutureUsecaseWithParams<void, String> {
  GetSeatsByMovieSessionId(this._eventHub);

  final EventHub _eventHub;
  final storage = const FlutterSecureStorage();

  @override
  ResultFuture<void> call(String params) async {

    await _eventHub.seatsUpdateSubscribe(params);

    return Right(null);

  }
}
