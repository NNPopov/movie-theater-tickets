// ignore_for_file: public_member_api_docs, sort_constructors_first

import 'package:equatable/equatable.dart';

class CinemaHall extends Equatable {
  final String id;
  final String description;

  const CinemaHall(this.id, this.description);

  @override
  List<Object?> get props => [id, description];
}

