import '../../domain/entities/movie_session.dart';

class MovieSessionDto extends MovieSession {


  MovieSessionDto({super.id, super.movieId, super.sessionDate, super.auditoriumId});

  MovieSessionDto.fromJson(Map<String, dynamic> json) {
    id = json['id'];
    movieId = json['movieId'];
    sessionDate = json['sessionDate'];
    auditoriumId = json['auditoriumId'];
  }

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = Map<String, dynamic>();
    data['id'] = id;
    data['movieId'] = movieId;
    data['sessionDate'] = sessionDate;
    data['auditoriumId'] = auditoriumId;
    return data;
  }
}

