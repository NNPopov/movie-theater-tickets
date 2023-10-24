

abstract class EventHub
{

  Future subscribe();

  Future shoppingCartUpdateSubscribe(String shoppingCartId);

  Future seatsUpdateSubscribe(String movieSessionId);

  Future unsubscribe();

}