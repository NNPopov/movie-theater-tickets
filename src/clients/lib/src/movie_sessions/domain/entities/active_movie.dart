// ignore_for_file: public_member_api_docs, sort_constructors_first

import 'package:equatable/equatable.dart';

class ActiveMovie extends Equatable {
  final String id;
  final String title;

  const ActiveMovie(this.id, this.title);

  ActiveMovie.empty()
      : this(
      '',
      '',
  );

  @override
  List<Object?> get props => [id, title];


}

