part of 'seat_cubit.dart';

@immutable
class SeatState extends Equatable {
  SeatState({required this.seats, required this.status, this.errorMessage})
    : byId = buildSeatIndex(seats);

  final List<Seat> seats;
  final String? errorMessage;
  final SeatStateStatus status;

  /// Derived O(1) status index keyed by (row, seatNumber), built once per
  /// instance from [seats]. Excluded from equality on purpose: it is a pure
  /// function of [seats], so it never changes equality semantics. `copyWith`
  /// recomputes it for free on every new instance.
  final Map<SeatId, Seat> byId;

  @override
  List<Object> get props => [seats, status];

  SeatState copyWith({
    List<Seat>? seats,
    SeatStateStatus? status,
    String? errorMessage,
  }) {
    return SeatState(
      seats: seats ?? this.seats,
      status: status ?? this.status,
      errorMessage: errorMessage,
    );
  }

  static SeatState initState() {
    return SeatState(seats: const [], status: SeatStateStatus.initial);
  }
}

enum SeatStateStatus { initial, fetching, loaded, error }

// class GettingSeats extends SeatState {
//   const GettingSeats(super.seats);
// }
//
// class SeatsState extends SeatState {
//   const SeatsState(super.seats);
//
// }
//
// class SeatsError extends SeatState {
//   const SeatsError(super.seats, this.message);
//
//   final String message;
//
//   @override
//   List<Object> get props => [super.seats, message];
// }
//
// class InitialState extends SeatState {
//   const InitialState(super.seats);
// }
