import 'dart:async';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:get_it/get_it.dart';

import '../../domain/entities/movie_session.dart';
import '../../domain/usecase/get_movie_sessions.dart';

part 'movie_session_state.dart';

GetIt getIt = GetIt.instance;

class MovieSessionCubit extends Cubit<MovieSessionState> {
  MovieSessionCubit({GetMovieSessions? getMovieSessions})
      : _getMovieSessions = getMovieSessions ?? getIt.get<GetMovieSessions>(),
        super(const InitialState());

  late GetMovieSessions _getMovieSessions;

  Future<void> getMovieSessions(String movieId) async {
    emit(const GettingMovieSession());

    final result = await _getMovieSessions(movieId);

    result.fold(
      (failure) => emit(MovieSessionError(failure.errorMessage)),
      (questions) => emit(MovieSessionsLoaded(questions)),
    );
  }
}
