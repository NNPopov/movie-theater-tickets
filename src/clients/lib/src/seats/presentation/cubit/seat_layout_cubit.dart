import 'package:equatable/equatable.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../cinema_halls/domain/layout/seat_layout.dart';
import '../../../cinema_halls/domain/ports/seat_layout_source.dart';

part 'seat_layout_state.dart';

/// Render-agnostic legacy Cubit that fetches and holds the [SeatLayout].
///
/// Depends only on the [SeatLayoutSource] geometry port — the seam that keeps the
/// future backend cutover free (N10/N22). A single `load` is naturally a Cubit,
/// matching the other legacy loaders on the seats screen.
class SeatLayoutCubit extends Cubit<SeatLayoutState> {
  SeatLayoutCubit(this._source) : super(const SeatLayoutState());

  final SeatLayoutSource _source;

  Future<void> load(String hallId) async {
    emit(state.copyWith(status: SeatLayoutStatus.loading));

    final result = await _source.getLayout(hallId);

    result.fold(
      (failure) => emit(
        state.copyWith(
          status: SeatLayoutStatus.error,
          errorMessage: failure.errorMessage,
        ),
      ),
      (layout) =>
          emit(state.copyWith(status: SeatLayoutStatus.loaded, layout: layout)),
    );
  }
}
