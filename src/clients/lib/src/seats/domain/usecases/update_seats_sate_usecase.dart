import '../../../../core/common/usecase.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../hub/app_events.dart';
import '../../../../core/buses/event_bus.dart';
import '../entities/seat.dart';
import 'package:dartz/dartz.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class UpdateSeatsStateUseCase
    extends FutureUsecaseWithParams<bool, List<Seat>> {
  UpdateSeatsStateUseCase(this._eventBus);

  late final EventBus _eventBus;
  final storage = const FlutterSecureStorage();

  @override
  ResultFuture<bool> call(List<Seat> params) async {
    try {
      _eventBus.send(SeatsUpdateEvent(params));

      return const Right(true);
    } on Exception catch (e) {
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    }
  }
}
