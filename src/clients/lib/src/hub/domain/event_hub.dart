

import '../../shopping_carts/data/models/seat_info_dto.dart';
import '../../shopping_carts/data/models/select_seat_dto.dart';
import '../presentation/cubit/connectivity_bloc.dart';

abstract class EventHub
{

  Future subscribe();

  Future shoppingCartUpdateSubscribe(String shoppingCartId);

  Future shoppingCartRemoveSubscribe(String shoppingCartId);

  Future seatsUpdateSubscribe(String movieSessionId);

  Future unsubscribe();

  Stream<ConnectivityEvent> get status;

  Future seatSelect(SeatInfoDto seatSelectRequestDto);

  Future seatUnselect(SeatInfoDto seatSelectRequestDto);

}