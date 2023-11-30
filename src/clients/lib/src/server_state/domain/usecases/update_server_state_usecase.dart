import 'package:movie_theater_tickets/src/server_state/domain/entities/server_state.dart';

import '../../../../core/common/usecase.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/typedefs.dart';
import '../../../../core/buses/event_bus.dart';

import 'package:dartz/dartz.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class UpdateServerStateUseCase
    extends FutureUsecaseWithParams<bool, ServerState> {
  UpdateServerStateUseCase(this._eventBus);

  late final EventBus _eventBus;
  final storage = const FlutterSecureStorage();

  @override
  ResultFuture<bool> call(ServerState params) async {
    try {
      _eventBus.send(params);

      return const Right(true);
    } on Exception catch (e) {
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    }
  }
}
