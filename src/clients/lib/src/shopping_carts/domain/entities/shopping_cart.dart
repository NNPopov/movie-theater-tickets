import 'package:equatable/equatable.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/entities/seat.dart';


class ShoppingCart extends Equatable {
  final int? maxNumberOfSeats;
  final DateTime? createdCard;
  final String? id;
  final String? movieSessionId;
  final  int? status;
  final List<Seat?>? seats;

 const ShoppingCart(
      {this.maxNumberOfSeats,
      this.createdCard,
      this.id,
      this.movieSessionId,
      this.status,
      this.seats});



  @override
  // TODO: implement props
  List<Object?> get props =>
      [maxNumberOfSeats, createdCard, id, movieSessionId, status, seats];
}


