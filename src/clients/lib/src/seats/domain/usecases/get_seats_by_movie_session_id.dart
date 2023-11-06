import '../../../../core/common/usecase.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../helpers/constants.dart';
import '../../../hub/domain/event_hub.dart';
import '../entities/seat.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:collection/collection.dart';
import 'package:dartz/dartz.dart';
import '../repos/seat_repo.dart';

GetIt getIt = GetIt.instance;

class GetSeatsByMovieSessionId
    extends FutureUsecaseWithParams<List<List<Seat>>, String> {

  GetSeatsByMovieSessionId({SeatRepo? repo, EventHub? eventHub})
  {
    _repo = repo ?? getIt.get<SeatRepo>();
    _eventHub = eventHub ?? getIt.get<EventHub>();
  }

  late SeatRepo _repo;
  late EventHub _eventHub;
  final storage = const FlutterSecureStorage();

  @override
  ResultFuture<List<List<Seat>>> call(String params) async
  {

    await _eventHub.seatsUpdateSubscribe(params);

    var hashId =( await storage.read(key: Constants.SHOPPING_CARD_HASH_ID))?? '';

    var result =  await _repo.getSeatsByMovieSessionId(params);

  return  result.fold((failure) => Left(failure),
            (seats) async {
      var finalSeats = seats.map((e) {
        var s = Seat.temp(
            row: e.row,
            seatNumber: e.seatNumber,
            blocked: e.blocked,
            isCurrentReserve: checkIsCurrentReserve(e, hashId),
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

  bool checkIsCurrentReserve(Seat e, String hashId) {
    if (hashId.isEmpty) {
      return false;
    }
    if (e.hashId == hashId) {
      return true;
    }
    return false;
  }
}