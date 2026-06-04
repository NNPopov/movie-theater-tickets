import '../../../../core/utils/typedefs.dart';
import '../layout/seat_layout.dart';

/// The single path to a hall's geometry, shaped after the eventual backend
/// endpoint `GET …/halls/{id}/layout → SeatLayout`.
///
/// Consumers depend only on this port; whether the layout is synthesized from
/// the legacy grid (today) or deserialized from the backend (later) is hidden.
abstract class SeatLayoutSource {
  const SeatLayoutSource();

  ResultFuture<SeatLayout> getLayout(String hallId);
}
