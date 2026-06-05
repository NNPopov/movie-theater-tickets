part of 'seat_layout_cubit.dart';

enum SeatLayoutStatus { initial, loading, loaded, error }

@immutable
class SeatLayoutState extends Equatable {
  const SeatLayoutState({
    this.status = SeatLayoutStatus.initial,
    this.layout,
    this.errorMessage,
  });

  final SeatLayoutStatus status;
  final SeatLayout? layout;
  final String? errorMessage;

  SeatLayoutState copyWith({
    SeatLayoutStatus? status,
    SeatLayout? layout,
    String? errorMessage,
  }) {
    return SeatLayoutState(
      status: status ?? this.status,
      layout: layout ?? this.layout,
      errorMessage: errorMessage,
    );
  }

  @override
  List<Object?> get props => [status, layout, errorMessage];
}
