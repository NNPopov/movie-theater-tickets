import 'package:equatable/equatable.dart';

class MovieSession extends Equatable {
  final String id;
  final String movieId;
  final DateTime sessionDate;
  final String cinemaHallId;

  const MovieSession(this.id, this.movieId, this.sessionDate, this.cinemaHallId);

  @override
  List<Object?> get props => [id];
}