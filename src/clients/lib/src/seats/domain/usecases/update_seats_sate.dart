import '../../../../core/common/usecase.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../helpers/constants.dart';
import '../../../hub/app_events.dart';
import '../../../../core/buses/event_bus.dart';
import '../entities/seat.dart';
import 'package:get_it/get_it.dart';

import 'package:dartz/dartz.dart';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:collection/collection.dart';

GetIt getIt = GetIt.instance;

class UpdateSeatsState extends FutureUsecaseWithParams<bool, List<Seat>> {
  UpdateSeatsState({EventBus? eventBus})
      : _eventBus = eventBus ?? getIt.get<EventBus>();

  late final EventBus _eventBus;
  final storage = const FlutterSecureStorage();

  @override
  ResultFuture<bool> call(List<Seat> params) async {
    try {
      var hashId =
          (await storage.read(key: Constants.SHOPPING_CARD_HASH_ID)) ?? '';

      var finalSeats = params.map((e) {
        var s = Seat.temp(
            row: e.row,
            seatNumber: e.seatNumber,
            blocked: e.blocked,
            isCurrentReserve: checkIsCurrentReserve(e, hashId),
            seatStatus: e.seatStatus,
            hashId: e.hashId);

        return s;
      });

      final rowSeats = groupBy(finalSeats, (seat) => seat.row)
          .values
          .map((seats) => seats.toList()..sort((a, b) => a.seatNumber - b.seatNumber))
          .toList()..sort((a, b) => a[0].row - b[0].row);

      _eventBus.send(SeatsUpdateEvent(rowSeats));

      return Right(true);
    } on Exception catch (e) {
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    }
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
