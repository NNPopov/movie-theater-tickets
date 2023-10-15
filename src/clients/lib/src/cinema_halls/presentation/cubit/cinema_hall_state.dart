part of 'cinema_hall_cubit.dart';

abstract class CinemaHallState  extends Equatable {
const CinemaHallState();

  @override
  List<Object> get props => [];

}


class GettingCinemaHall extends CinemaHallState {
  const GettingCinemaHall();
}

class CinemaHallLoaded extends CinemaHallState {
  const CinemaHallLoaded(this.auditorium);

  final CinemaHall auditorium;

  @override
  List<Object> get props => [auditorium];
}



class CinemaHallError extends CinemaHallState {
  const CinemaHallError(this.message);

  final String message;

  @override
  List<Object> get props => [message];
}

class InitialState extends CinemaHallState {
  const InitialState();
}
