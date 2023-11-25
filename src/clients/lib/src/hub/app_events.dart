import 'package:equatable/equatable.dart';
import '../seats/domain/entities/seat.dart';


abstract class AppEvent  extends Equatable {
  const AppEvent();

  @override
  List<Object> get props => [];

}

class SeatsUpdateEvent extends AppEvent
{
  const SeatsUpdateEvent(this.seats);

  final List<Seat> seats;

  @override
  List<Object> get props => [seats];
}


class ShoppingCartHashIdUpdated extends AppEvent{}