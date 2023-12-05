import 'dart:async' as domain;
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter/cupertino.dart';
import '../../../seats/domain/usecases/get_cinema_hall_info.dart';
import '../../domain/entity/cinema_hall_info.dart';

part 'movie_state.dart';

class CinemaHallInfoCubit extends Cubit<CinemaHallInfoState> {
  CinemaHallInfoCubit(this._getCinemaHallInfo) : super(CinemaHallInfoState.initial());

  late final GetCinemaHallInfo _getCinemaHallInfo;

  domain.Future<void> getMovieById(String cinemaHallId) async {
    emit(CinemaHallInfoState.fetching());
    final result = await _getCinemaHallInfo(cinemaHallId);

    result.fold(
        (failure) => emit(state.copyWith(
            status: CinemaHallInfoStatus.error, errorMessage: failure.errorMessage)),
        (movie) =>
            emit(state.copyWith(movie: movie, status: CinemaHallInfoStatus.completed)));
  }
}
