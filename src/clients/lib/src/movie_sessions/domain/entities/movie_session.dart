import 'package:equatable/equatable.dart';

class MovieSession extends Equatable {
  final String id;
  final String movieId;
  final DateTime sessionDate;
  final String auditoriumId;

  MovieSession(this.id, this.movieId, this.sessionDate, this.auditoriumId);

  @override
  List<Object?> get props => [id];
}