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
    extends FutureUsecaseWithParams<List<Seat>, String> {
  GetSeatsByMovieSessionId(this._repo, this._eventHub);

  final SeatRepo _repo;
  final EventHub _eventHub;
  final storage = const FlutterSecureStorage();

  @override
  ResultFuture<List<Seat>> call(String params) async {

    await _eventHub.seatsUpdateSubscribe(params);

    var result = await _repo.getSeatsByMovieSessionId(params);

    return result.fold((failure) => Left(failure), (seats) async {
      return Right(seats);
    });
  }
}
