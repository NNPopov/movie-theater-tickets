part of 'movie_session_cubit.dart';

abstract class MovieSessionState  extends Equatable {
const MovieSessionState();

  @override
  List<Object> get props => [];

}


class GettingMovieSession extends MovieSessionState {
  const GettingMovieSession();
}

class MovieSessionsLoaded extends MovieSessionState {
  const MovieSessionsLoaded(this.movieSession);

  final List<MovieSession> movieSession;

  @override
  List<Object> get props => [movieSession];
}

class MovieSessionError extends MovieSessionState {
  const MovieSessionError(this.message);

  final String message;

  @override
  List<Object> get props => [message];
}

class InitialState extends MovieSessionState {
  const InitialState();
}