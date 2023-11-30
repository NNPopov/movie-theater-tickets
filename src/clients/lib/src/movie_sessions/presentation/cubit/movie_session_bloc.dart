import 'dart:async';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter/cupertino.dart';
import 'package:get_it/get_it.dart';

import '../../domain/entities/movie_session.dart';
import '../../domain/usecase/get_movie_sessions.dart';

part 'movie_session_state.dart';

GetIt getIt = GetIt.instance;

class MovieSessionBloc extends Bloc<MovieSessionEvent, MovieSessionState> {
  MovieSessionBloc(this._getMovieSessionsUseCase)
      : super(MovieSessionState(status: MovieSessionStateStatus.initial)) {
    on<MovieSessionEvent>(_getMovieSessions);
  }

  late final GetMovieSessions _getMovieSessionsUseCase;

  Future<void> _getMovieSessions(
    MovieSessionEvent event,
    Emitter<MovieSessionState> emit,
  ) async {
    emit(state.copyWith(status: MovieSessionStateStatus.fetching));

    final result = await _getMovieSessionsUseCase(event.movieId);

    result.fold(
      (failure) => emit(state.copyWith(
          status: MovieSessionStateStatus.error,
          errorMessage: failure.errorMessage)),
      (movieSessions) => emit(state.copyWith(
          movieSession: movieSessions, status: MovieSessionStateStatus.loaded)),
    );
  }
}
