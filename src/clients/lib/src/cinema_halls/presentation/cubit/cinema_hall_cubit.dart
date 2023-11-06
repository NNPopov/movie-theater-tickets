import 'dart:async';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:get_it/get_it.dart';

import '../../domain/entity/cinema_hall.dart';
import '../../domain/usecases/get_cinema_hall.dart';

part 'cinema_hall_state.dart';

GetIt getIt = GetIt.instance;

class CinemaHallCubit extends Cubit<CinemaHallState> {
  CinemaHallCubit({GetCinemaHallById? getCinemaHall})
      : _getCinemaHallById = getCinemaHall ?? getIt.get<GetCinemaHallById>(),
        super(const InitialState());

  late GetCinemaHallById _getCinemaHallById;

  Future<void> getAuditorium(String auditoriumId) async {
    emit(const GettingCinemaHall());

    final result = await _getCinemaHallById(auditoriumId);
    result.fold((failure) => emit(CinemaHallError(failure.errorMessage)),
        (cinemaSession) => emit(CinemaHallLoaded(cinemaSession)));
  }
}
