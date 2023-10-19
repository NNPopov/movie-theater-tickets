import 'package:equatable/equatable.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/entities/seat.dart';
import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';
import '../../../../core/utils/typedefs.dart';

class ShoppingCart extends Equatable {
  final int? maxNumberOfSeats;
  final DateTime? createdCard;
  final String? id;
  final String? movieSessionId;
  final int? status;
  late List<ShoppingCartSeat> shoppingCartSeat;

  ShoppingCart(
      {this.maxNumberOfSeats,
      this.createdCard,
      this.id,
      this.movieSessionId,
      this.status,
      List<ShoppingCartSeat>? seats}) {
    shoppingCartSeat = seats ?? [];
  }

  Either<Failure, void> addSeat(ShoppingCartSeat seat) {
    if (shoppingCartSeat.length < maxNumberOfSeats!) {
      shoppingCartSeat.add(seat);
      return const Right(null);
    }
    return  Left(DataFailure(message:"Max number of Seats is ${maxNumberOfSeats}", statusCode: 500));
  }

  void deleteSeat(ShoppingCartSeat seat) {
    shoppingCartSeat.remove(seat);
  }

  @override
  List<Object?> get props => [
        maxNumberOfSeats,
        createdCard,
        id,
        movieSessionId,
        status,
        shoppingCartSeat
      ];
}
