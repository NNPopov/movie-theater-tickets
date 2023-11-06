part of 'movie_theater_cubit.dart';

abstract class MovieTheaterState  extends Equatable {
const MovieTheaterState();

  @override
  List<Object> get props => [];

}


class GettingMovie extends MovieTheaterState {
  const GettingMovie();
}

class MovieLoaded extends MovieTheaterState {
  const MovieLoaded(this.movie);

  final Movie movie;

  @override
  List<Object> get props => [movie];
}


class GettingMovies extends MovieTheaterState {
  const GettingMovies();
}



class MoviesLoaded extends MovieTheaterState {
  const MoviesLoaded(this.movies);

  final List<Movie> movies;

  @override
  List<Object> get props => [movies];
}

class MovieTheaterError extends MovieTheaterState {
  const MovieTheaterError(this.message);

  final String message;

  @override
  List<Object> get props => [message];
}

class InitialState extends MovieTheaterState {
  const InitialState();
}