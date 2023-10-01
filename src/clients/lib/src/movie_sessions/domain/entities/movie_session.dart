import 'package:equatable/equatable.dart';

class MovieSession extends Equatable {
  String? id;
  String? movieId;
  DateTime? sessionDate;
  String? auditoriumId;

  MovieSession({this.id, this.movieId, this.sessionDate, this.auditoriumId});

  @override
  List<Object?> get props => [id];
}