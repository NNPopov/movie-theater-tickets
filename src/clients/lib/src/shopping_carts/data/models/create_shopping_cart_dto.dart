// ignore_for_file: public_member_api_docs, sort_constructors_first
import 'dart:convert';

import 'package:equatable/equatable.dart';

class CreateShoppingCartDto extends Equatable {

  final int maxNumberOfSeats;

  const CreateShoppingCartDto(
    this.maxNumberOfSeats,
  );

  @override
  List<Object> get props => [maxNumberOfSeats];


  CreateShoppingCartDto copyWith({
    int? maxNumberOfSeats,
  }) {
    return CreateShoppingCartDto(
      maxNumberOfSeats ?? this.maxNumberOfSeats,
    );
  }

  Map<String, dynamic> toMap() {
    return <String, dynamic>{
      'maxNumberOfSeats': maxNumberOfSeats,
    };
  }

  factory CreateShoppingCartDto.fromMap(Map<String, dynamic> map) {
    return CreateShoppingCartDto(
      map['maxNumberOfSeats'] as int,
    );
  }

  String toJson() => json.encode(toMap());

  factory CreateShoppingCartDto.fromJson(String source) => CreateShoppingCartDto.fromMap(json.decode(source) as Map<String, dynamic>);

  @override
  bool get stringify => true;
}
