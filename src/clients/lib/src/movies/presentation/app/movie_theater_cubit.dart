import 'dart:async' as domain;
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:get_it/get_it.dart';
import 'package:movie_theater_tickets/src/movies/domain/entities/movie.dart';
import '../../domain/usecases/get_movie_by_id.dart';
import '../../domain/usecases/get_movies.dart';

part 'movie_theater_state.dart';

GetIt getIt = GetIt.instance;

class MovieTheaterCubit extends Cubit<MovieTheaterState> {
  MovieTheaterCubit({GetMovies? getMovies, GetMovieById? getMovieById})
      : _getMovies = getMovies ?? getIt.get<GetMovies>(),
        _getMovieById = getMovieById ?? getIt.get<GetMovieById>(),
        super(const InitialState());

  late GetMovies _getMovies;
  late GetMovieById _getMovieById;

  domain.Future<void> getMovies() async {
    emit(const GettingMovies());
    final result = await _getMovies();

    result.fold(
      (failure) => emit(MovieTheaterError(failure.errorMessage)),
      (questions) => emit(MoviesLoaded(questions)),
    );
  }

  domain.Future<void> getMovieById(String movieId) async {
    emit(const GettingMovie());
    final result = await _getMovieById(movieId);

    result.fold(
        (failure) => emit(MovieTheaterError(failure.errorMessage)),
        (movie) =>
            emit(MovieLoaded(movie)));
  }
}
