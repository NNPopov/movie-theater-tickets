import 'dart:async' as domain;
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter/cupertino.dart';
import 'package:movie_theater_tickets/src/movies/domain/entities/movie.dart';
import '../../../movies/domain/usecases/get_movies.dart';
import '../../domain/entities/active_movie.dart';

part 'movie_theater_state.dart';

class MovieTheaterCubit extends Cubit<MovieTheaterState> {
  MovieTheaterCubit(this._getActiveMovies)
      : super( MovieTheaterState.initial());

  late final GetActiveMovies _getActiveMovies;

  Future<void> getMovies() async {
    emit(MovieTheaterState.fetching());


    final result = await _getActiveMovies();

    result.fold(
      (failure) => emit(state.copyWith(
          status: MoviesStatus.error, errorMessage: failure.errorMessage)),
      (movies) => emit(state.copyWith(movies: movies, status: MoviesStatus.completed)),
    );
  }
}


