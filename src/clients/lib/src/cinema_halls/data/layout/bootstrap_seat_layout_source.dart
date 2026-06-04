/// TEMPORARY BOOTSTRAP — delete when the backend serves GET …/halls/{id}/layout.
///
/// Fakes that endpoint by synthesizing a [SeatLayout] from the legacy hall grid
/// via [synthesizeLegacyLayout]. When the backend serves a real [SeatLayout],
/// delete this file and bind [SeatLayoutSource] to an adapter that deserializes
/// the backend JSON through `SeatLayout.fromJson` instead.
library;

import 'package:dartz/dartz.dart';

import '../../../../core/common/app_logger.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/typedefs.dart';
import '../../domain/layout/seat_layout.dart';
import '../../domain/ports/seat_layout_source.dart';
import '../../domain/repo/cinema_hall_repo.dart';
import 'legacy_seat_layout_synthesizer.dart';

class BootstrapSeatLayoutSource implements SeatLayoutSource {
  BootstrapSeatLayoutSource(this._halls, {AppLogger? logger})
    : _logger = logger ?? getLogger(BootstrapSeatLayoutSource);

  final CinemaHallRepo _halls;
  final AppLogger _logger;

  @override
  ResultFuture<SeatLayout> getLayout(String hallId) async {
    try {
      final info = await _halls.getCinemaHallInfoById(hallId);
      return info.fold(
        Left.new, // pass a known legacy failure through unchanged
        (hall) => Right(synthesizeLegacyLayout(hall)),
      );
    } catch (e, st) {
      _logger.e(
        'Failed to synthesize SeatLayout for hall $hallId',
        error: e,
        stackTrace: st,
      );
      return Left(ServerFailure(message: e.toString(), statusCode: 500));
    }
  }
}
