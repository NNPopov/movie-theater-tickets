import 'dart:async' as domain;
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter/cupertino.dart';
import 'package:movie_theater_tickets/src/movies/domain/entities/movie.dart';
import '../../domain/usecases/get_movies.dart';

part 'movie_theater_state.dart';

class MovieTheaterCubit extends Cubit<MovieTheaterState> {
  MovieTheaterCubit(this._getMovies)
      : super( MovieTheaterState.initial());

  late final GetMovies _getMovies;

  domain.Future<void> getMovies() async {
    emit(MovieTheaterState.fetching());
    final result = await _getMovies();

    result.fold(
      (failure) => emit(state.copyWith(
          status: MoviesStatus.error, errorMessage: failure.errorMessage)),
      (movies) => emit(state.copyWith(movies: movies, status: MoviesStatus.completed)),
    );
  }
}
