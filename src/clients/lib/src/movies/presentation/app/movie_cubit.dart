import 'dart:async' as domain;
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter/cupertino.dart';
import 'package:movie_theater_tickets/src/movies/domain/entities/movie.dart';
import '../../domain/usecases/get_movie_by_id.dart';
import 'movie_theater_cubit.dart';

part 'movie_state.dart';

class MovieCubit extends Cubit<MovieState> {
  MovieCubit(this._getMovieById) : super(MovieState.initial());

  late final GetMovieById _getMovieById;

  domain.Future<void> getMovieById(String movieId) async {
    emit(MovieState.fetching());
    final result = await _getMovieById(movieId);

    result.fold(
        (failure) => emit(state.copyWith(
            status: MoviesStatus.error, errorMessage: failure.errorMessage)),
        (movie) =>
            emit(state.copyWith(movie: movie, status: MoviesStatus.completed)));
  }
}
