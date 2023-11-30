// ignore_for_file: public_member_api_docs, sort_constructors_first

import 'package:equatable/equatable.dart';

class Movie extends Equatable {
  final String id;
  final String title;
  final String imdbId;
  final String stars;
  final DateTime releaseDate;

  const Movie(this.id, this.title, this.imdbId, this.stars, this.releaseDate);

  Movie.empty()
      : this(
      '',
      '',
       '',
      '',
      DateTime.parse('1900-01-01')
  );

  @override
  List<Object?> get props => [id, title, imdbId, stars, releaseDate];


}

