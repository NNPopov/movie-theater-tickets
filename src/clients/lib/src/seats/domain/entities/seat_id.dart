/// Identity of a seat within a hall: its (row, seat-number) coordinate.
///
/// Shared by the live-status index (P1) and explicit geometry (P2/P5).
/// Structural equality + hashing come for free from the Dart record type —
/// no `Equatable`, no boilerplate.
typedef SeatId = (int row, int seatNumber);
