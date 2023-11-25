part of 'seat_cubit.dart';

abstract class SeatState  extends Equatable {
const SeatState(this.seats);


final List<Seat> seats;
  @override
  List<Object> get props => [seats];

}


class GettingSeats extends SeatState {
  const GettingSeats(super.seats);
}

class SeatsState extends SeatState {
  const SeatsState(super.seats);

}

class SeatsError extends SeatState {
  const SeatsError(super.seats, this.message);

  final String message;

  @override
  List<Object> get props => [super.seats, message];
}

class InitialState extends SeatState {
  const InitialState(super.seats);
}
