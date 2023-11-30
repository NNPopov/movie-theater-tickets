part of 'movie_cubit.dart';

@immutable
class MovieState extends Equatable {
  MovieState({required this.movie, required this.status, this.errorMessage});

  final Movie movie;
  final MoviesStatus status;
  late String? errorMessage;

  static MovieState initial() {
    return MovieState(movie: Movie.empty(), status: MoviesStatus.initial);
  }

  static MovieState fetching() {
    return MovieState(movie: Movie.empty(), status: MoviesStatus.fetching);
  }

  @override
  List<Object> get props => [movie, status];

  MovieState copyWith({
    Movie? movie,
    MoviesStatus? status,
    String? errorMessage,
  }) {
    return MovieState(
      movie: movie ?? this.movie,
      status: status ?? this.status,
      errorMessage: errorMessage,
    );
  }
}

//
//
// class GettingMovie extends MovieTheaterState {
//   const GettingMovie();
// }
//
// class MovieLoaded extends MovieTheaterState {
//   const MovieLoaded(this.movie);
//
//   final Movie movie;
//
//   @override
//   List<Object> get props => [movie];
// }
//
// class GettingMovies extends MovieTheaterState {
//   const GettingMovies();
// }
//
// class MoviesLoaded extends MovieTheaterState {
//   const MoviesLoaded(this.movies);
//
//   final List<Movie> movies;
//
//   @override
//   List<Object> get props => [movies];
// }
//
// class MovieTheaterError extends MovieTheaterState {
//   const MovieTheaterError(this.message);
//
//   final String message;
//
//   @override
//   List<Object> get props => [message];
// }
//
// class InitialState extends MovieTheaterState {
//   const InitialState();
// }
