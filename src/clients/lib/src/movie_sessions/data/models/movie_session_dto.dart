import '../../domain/entities/movie_session.dart';

class MovieSessionDto extends MovieSession {
  MovieSessionDto(
      super.id, super.movieId, super.sessionDate, super.auditoriumId);

  factory MovieSessionDto.fromMap(Map<String, dynamic> map) {
    return MovieSessionDto(
        map['id'],
        map['movieId'],
        DateTime.parse(map['sessionDate']),
        map['auditoriumId']);
  }

  factory MovieSessionDto.fromJson(dynamic source) =>
      MovieSessionDto.fromMap(source as Map<String, dynamic>);

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = Map<String, dynamic>();
    data['id'] = id;
    data['movieId'] = movieId;
    data['sessionDate'] = sessionDate;
    data['auditoriumId'] = auditoriumId;
    return data;
  }
}
