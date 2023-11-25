

import '../presentation/cubit/connectivity_bloc.dart';

abstract class EventHub
{

  Future subscribe();

  Future shoppingCartUpdateSubscribe(String shoppingCartId);

  Future seatsUpdateSubscribe(String movieSessionId);

  Future unsubscribe();

  Stream<ConnectivityEvent> get status;

}