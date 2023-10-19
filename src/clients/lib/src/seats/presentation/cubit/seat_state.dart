part of 'seat_cubit.dart';

abstract class SeatState  extends Equatable {
const SeatState();

  @override
  List<Object> get props => [];

}


class GettingSeats extends SeatState {
  const GettingSeats();
}

class SeatsState extends SeatState {
  const SeatsState(this.seats);

  final List<Seat> seats;

  @override
  List<Object> get props => [seats];
}

class SeatsError extends SeatState {
  const SeatsError(this.message);

  final String message;

  @override
  List<Object> get props => [message];
}

class InitialState extends SeatState {
  const InitialState();
}
