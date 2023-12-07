// ignore_for_file: public_member_api_docs, sort_constructors_first

import 'package:equatable/equatable.dart';

import 'cinema_seat.dart';

class CinemaHallInfo extends Equatable {
  final String id;
  final String description;

  final List<List<CinemaSeat>> cinemaSeat;


  const CinemaHallInfo(this.id, this.description, this.cinemaSeat);


  CinemaHallInfo.empty()
      : this(
      '',
      '',
      []
  );

  @override
  List<Object?> get props => [id, description, cinemaSeat];
}

