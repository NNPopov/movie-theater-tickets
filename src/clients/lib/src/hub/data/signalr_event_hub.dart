import 'dart:convert';

import '../../seats/data/models/seat_dto.dart';
import '../../seats/domain/entities/seat.dart';
import '../../seats/domain/usecases/update_seats_sate.dart';
import '../../shopping_carts/data/models/shopping_cart_dto.dart';
import '../../shopping_carts/domain/entities/shopping_cart.dart';
import '../../shopping_carts/domain/usecases/update_state_shopping_cart.dart';
import '../connectivity/connectivity_bloc.dart';
import '../domain/event_hub.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:get_it/get_it.dart';

import '../../../core/buses/event_bus.dart';

GetIt getIt = GetIt.instance;

class SignalREventHub implements EventHub {
  SignalREventHub(
      {UpdateShoppingCartState? updateShoppingCartState,
      EventBus? eventBus,
      UpdateSeatsState? updateSeatsState}) {
    _updateShoppingCartState =
        updateShoppingCartState ?? getIt.get<UpdateShoppingCartState>();
    _updateSeatsState = updateSeatsState ?? getIt.get<UpdateSeatsState>();
    _eventBus = eventBus ?? getIt.get<EventBus>();
  }

  late HubConnection _hubConnection;
  late UpdateShoppingCartState _updateShoppingCartState;
  late UpdateSeatsState _updateSeatsState;
  late EventBus _eventBus;

  @override
  Future subscribe() async {
    final httpConnectionOptions = HttpConnectionOptions(
      logMessageContent: true,
      requestTimeout: 50000,
    );

    var baseUrl = dotenv.env["BASE_API_URL"].toString();

    _hubConnection = HubConnectionBuilder()
        .withUrl('${baseUrl}/cinema-hall-seats-hub',
            options: httpConnectionOptions)
        .withAutomaticReconnect()
        .build();

    _hubConnection.onreconnected(({connectionId}) {
      _eventBus.send(ConnectedEvent());
    });
    _hubConnection.onreconnecting(({error}) {
      _eventBus.send(DisconnectedEvent());
    });
    _hubConnection.onclose(({error}) {
      _eventBus.send(DisconnectedEvent());
      print("onclose called");
    });

    _hubConnection.on("SentShoppingCartState", _shoppingCartStateUpdate);

    _hubConnection.on("SentState", _seatsStateUpdate);

    if (_hubConnection.state != HubConnectionState.Connected) {
      await _hubConnection.start();
    }
  }

  Future<void> _shoppingCartStateUpdate(List<Object?>? args) async {
    var senderName = args?[0];
    var movies = jsonDecode(jsonEncode(senderName));

    ShoppingCart shoppingCartValue =
        ShoppingCartDto.fromJson(movies) as ShoppingCart;

    await _updateShoppingCartState(shoppingCartValue);
  }

  Future<void> _seatsStateUpdate(List<Object?>? args) async {
    List<dynamic> movies = jsonDecode(jsonEncode(args?[0]));

    List<Seat> seatDtos =
        movies.map((json) => SeatDto.fromJson(json) as Seat).toList();

    await _updateSeatsState(seatDtos);
  }

  @override
  Future unsubscribe() {
    // TODO: implement unsubscribe
    throw UnimplementedError();
  }

  @override
  Future seatsUpdateSubscribe(String movieSessionId) async {
    if (_hubConnection.state != HubConnectionState.Connected) {
      await _hubConnection.start();
    }
    await _hubConnection.invoke('JoinGroup', args: <Object>[movieSessionId]);
  }

  @override
  Future shoppingCartUpdateSubscribe(String shoppingCartId) async {
    if (_hubConnection.state != HubConnectionState.Connected) {
      await _hubConnection.start();
    }
    await _hubConnection
        .invoke('RegisterShoppingCart', args: <Object>[shoppingCartId]);
  }
}
