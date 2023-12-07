part of 'movie_cubit.dart';

@immutable
class CinemaHallInfoState extends Equatable {
  CinemaHallInfoState({required this.movie, required this.status, this.errorMessage});

  final CinemaHallInfo movie;
  final CinemaHallInfoStatus status;
  late String? errorMessage;

  static CinemaHallInfoState initial() {
    return CinemaHallInfoState(movie: CinemaHallInfo.empty(), status: CinemaHallInfoStatus.initial);
  }

  static CinemaHallInfoState fetching() {
    return CinemaHallInfoState(movie: CinemaHallInfo.empty(), status: CinemaHallInfoStatus.fetching);
  }

  @override
  List<Object> get props => [movie, status];

  CinemaHallInfoState copyWith({
    CinemaHallInfo? movie,
    CinemaHallInfoStatus? status,
    String? errorMessage,
  }) {
    return CinemaHallInfoState(
      movie: movie ?? this.movie,
      status: status ?? this.status,
      errorMessage: errorMessage,
    );
  }
}

enum CinemaHallInfoStatus {
fetching, initial, error, completed}
