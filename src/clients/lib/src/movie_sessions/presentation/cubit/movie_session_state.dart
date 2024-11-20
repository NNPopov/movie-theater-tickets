part of 'movie_session_bloc.dart';

class MovieSessionEvent extends Equatable {
  const MovieSessionEvent({required this.movieId});

  final String movieId;

  @override
  List<Object> get props => [movieId];
}

@immutable
class MovieSessionState extends Equatable {
  MovieSessionState(
      {this.movieSession = const [],
      required this.status,
      this.errorMessage = ''});

  final List<List<List<MovieSession>>> movieSession;
  final MovieSessionStateStatus status;
  final String? errorMessage;

  MovieSessionState copyWith(
      {List<List<List<MovieSession>>>? movieSession,
      MovieSessionStateStatus? status,
      String? errorMessage}) {
    return MovieSessionState(
      movieSession: movieSession ?? this.movieSession,
      status: status ?? this.status,
      errorMessage: errorMessage,
    );
  }

  @override
  List<Object> get props => [movieSession, status];
}

enum MovieSessionStateStatus { initial, fetching, loaded, error }
