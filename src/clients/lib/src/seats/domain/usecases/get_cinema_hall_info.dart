import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../hub/domain/event_hub.dart';
import '../entities/seat.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:collection/collection.dart';
import 'package:dartz/dartz.dart';
import '../repos/seat_repo.dart';

GetIt getIt = GetIt.instance;

class GetCinemaHallInfo
    extends FutureUsecaseWithParams<List<List<Seat>>, String> {

  GetCinemaHallInfo(this._repo, this._eventHub);

  final SeatRepo _repo;
  final EventHub _eventHub;
  final storage = const FlutterSecureStorage();

  @override
  ResultFuture<List<List<Seat>>> call(String params) async
  {
    await _eventHub.seatsUpdateSubscribe(params);

    var result =  await _repo.getSeatsByMovieSessionId(params);

  return  result.fold((failure) => Left(failure),
            (seats) async {
      var finalSeats = seats.map((e) {
        var s = Seat.temp(
            row: e.row,
            seatNumber: e.seatNumber,
            blocked: e.blocked,
            isCurrentReserve: true,
            seatStatus: e.seatStatus,
            hashId: e.hashId);

        return s;
      }).toList();

      final rowSeats = groupBy(finalSeats, (seat) => seat.row)
          .values
          .map((seats) => seats.toList()..sort((a, b) => a.seatNumber - b.seatNumber))
          .toList()..sort((a, b) => a[0].row - b[0].row);

      //_eventBus.send(SeatsUpdateEvent(rowSeats));

      return Right(rowSeats);
    });
  }


}