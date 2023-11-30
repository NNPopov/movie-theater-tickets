part of 'movie_theater_cubit.dart';

@immutable
class MovieTheaterState extends Equatable {
  MovieTheaterState(
      {required this.movies, required this.status, this.errorMessage});

  final List<Movie> movies;
  final MoviesStatus status;
  late String? errorMessage;

  static MovieTheaterState initial() {
    return MovieTheaterState(movies: const [], status: MoviesStatus.initial);
  }

  static MovieTheaterState fetching() {
    return MovieTheaterState(movies: const [], status: MoviesStatus.fetching);
  }

  @override
  List<Object> get props => [movies, status];

  MovieTheaterState copyWith({
    List<Movie>? movies,
    MoviesStatus? status,
    String? errorMessage,
  }) {
    return MovieTheaterState(
      movies: movies ?? this.movies,
      status: status ?? this.status,
      errorMessage: errorMessage,
    );
  }
}

enum MoviesStatus { initial, fetching, error, completed }