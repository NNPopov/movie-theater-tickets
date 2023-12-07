import 'package:equatable/equatable.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/entities/price_calculation_result.dart';
import 'package:movie_theater_tickets/src/shopping_carts/domain/entities/seat.dart';
import 'package:dartz/dartz.dart';
import '../../../../core/errors/failures.dart';

class ShoppingCart extends Equatable {
  final int? maxNumberOfSeats;
  final DateTime? createdAt;
  final String? id;
  final String? movieSessionId;
  final bool? isAssigned;
  final ShoppingCartStatus? status;
  late List<ShoppingCartSeat> shoppingCartSeat;
  late PriceCalculationResult? priceCalculationResult;

  ShoppingCart(
      {this.maxNumberOfSeats,
      this.createdAt,
      this.id,
      this.movieSessionId,
      this.status,
      List<ShoppingCartSeat>? seats,
      this.isAssigned,
      this.priceCalculationResult}) {
    shoppingCartSeat = seats ?? [];
  }

  ShoppingCart.empty()
      : this(
            maxNumberOfSeats: 0,
            createdAt: DateTime.parse('1900-01-01'),
            id: '',
            movieSessionId: '',
            status: null,
            seats: null,
            isAssigned: false,
            priceCalculationResult: null);

  Either<Failure, void> addSeat(ShoppingCartSeat seat) {
    if (status != ShoppingCartStatus.InWork) {
      return Left(DataFailure(
          message: "ShoppingCart has status $status", statusCode: 500));
    }

    if (shoppingCartSeat.length < maxNumberOfSeats!) {
      if (shoppingCartSeat.any((e) =>
          e.seatRow == seat.seatRow && e.seatNumber == seat.seatNumber)) {
        return const Left(
            DataFailure(message: "Seat is alredy reserved", statusCode: 500));
      }
      shoppingCartSeat.add(seat);
      return const Right(null);
    }
    return Left(DataFailure(
        message: "Max number of Seats is $maxNumberOfSeats", statusCode: 500));
  }

  Either<Failure, void> deleteSeat(ShoppingCartSeat seat) {
    shoppingCartSeat.remove(seat);

    return const Right(null);
  }

  @override
  List<Object?> get props => [
        maxNumberOfSeats,
        createdAt,
        id,
        movieSessionId,
        status,
        shoppingCartSeat,
        isAssigned
      ];
}

enum ShoppingCartStatus { InWork, SeatsReserved, PurchaseCompleted, Deleted }
