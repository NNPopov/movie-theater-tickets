import 'package:equatable/equatable.dart';

class CreateShoppingCartResponse extends Equatable {


  final String shoppingCartId;

  final String hashId;

  const CreateShoppingCartResponse(this.shoppingCartId, this.hashId);

  CreateShoppingCartResponse.fromJson(Map<String, dynamic> json)
      : this(
      json['shoppingCartId'], json['hashId']);




  @override
  // TODO: implement props
  List<Object?> get props =>  [shoppingCartId];
}