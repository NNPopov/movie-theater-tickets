import 'dart:async';
import 'dart:convert';

import '../../helpers/constants.dart';
import '../../seats/data/models/seat_dto.dart';
import '../../seats/domain/entities/seat.dart';
import '../../seats/domain/usecases/update_seats_sate_usecase.dart';
import '../../shopping_carts/data/models/shopping_cart_dto.dart';
import '../../shopping_carts/domain/entities/shopping_cart.dart';
import '../../shopping_carts/domain/usecases/update_state_shopping_cart.dart';
import '../presentation/cubit/connectivity_bloc.dart';
import '../domain/event_hub.dart';
import 'package:signalr_netcore/signalr_client.dart';

import 'package:logging/logging.dart';

class SignalREventHub implements EventHub {
  SignalREventHub(
      {required this.updateShoppingCartState,

      required this.updateSeatsState});

  late HubConnection _hubConnection;
  late ShoppingCartUpdateStateUseCase updateShoppingCartState;
  late UpdateSeatsStateUseCase updateSeatsState;


  final _controller = StreamController<ConnectivityEvent>();

  @override
  Stream<ConnectivityEvent> get status async* {
    yield* _controller.stream;
  }

  late String? _movieSessionId = null;
  late String? _shoppingCartId = null;

  @override
  Future subscribe() async {
    _controller.add(ReconnectingEvent());

    final httpConnectionOptions = HttpConnectionOptions(
      logMessageContent: true,
      requestTimeout: 60000,
    );

    try {
      if (_hubConnection != null) {
        if (_hubConnection.state != HubConnectionState.Connected) {
          await _hubConnection.stop();
        }
      }
    } catch (e) {}

    Logger.root.level = Level.ALL;
    // Writes the log messages to the console
    Logger.root.onRecord.listen((LogRecord rec) {
      print('${rec.level.name}: ${rec.time}: ${rec.message}');
    });

    // If you want only to log out the message for the higer level hub protocol:
    final hubProtLogger = Logger("SignalR - hub");
// If youn want to also to log out transport messages:
    final transportProtLogger = Logger("SignalR - transport");

    var baseUrl = Constants.BASE_API_URL;

    _hubConnection = HubConnectionBuilder()
        .withUrl('$baseUrl/ws/cinema-hall-seats-hub',
            options: httpConnectionOptions)
        .withAutomaticReconnect(retryDelays: Constants.RETRY_POLICY)
        .configureLogging(transportProtLogger)
        .build();

    _hubConnection.onreconnected(({connectionId}) {
      _tryReconnect();
      _controller.add(ConnectedEvent());

      print("Connected called");
    });

    _hubConnection.onreconnecting(({error}) {
      _controller.add(ReconnectingEvent());
      print("Reconnecting called");
    });

    _hubConnection.onclose(({error}) {
      _controller.add(DisconnectedEvent());
      print("On Close called");
    });

    _hubConnection.on('SentShoppingCartState', _shoppingCartStateUpdate);

    _hubConnection.on('SentCinemaHallSeatsState', _seatsStateUpdate);

    try {
      await _tryStartHub();
      _controller.add(ConnectedEvent());
    } on Exception catch (e) {
      _controller.add(DisconnectedEvent());
    }
  }

  Future<void> _shoppingCartStateUpdate(List<Object?>? args) async {
    print('shoppingCartStateUpdate recived');
    var senderName = args?[0];
    var movies = jsonDecode(jsonEncode(senderName));

    ShoppingCart shoppingCartValue =
        ShoppingCartDto.fromJson(movies) as ShoppingCart;

    await updateShoppingCartState(shoppingCartValue);
  }

  Future<void> _seatsStateUpdate(List<Object?>? args) async {
    print('seatsStateUpdate recived');

    List<dynamic> movies = jsonDecode(jsonEncode(args?[0]));

    List<Seat> seatDtos =
        movies.map((json) => SeatDto.fromJson(json) as Seat).toList();

    await updateSeatsState(seatDtos);
  }

  @override
  Future unsubscribe() async {
    print('unsubscribe');
  }

  @override
  Future seatsUpdateSubscribe(String movieSessionId) async {
    _movieSessionId = movieSessionId;

    await _seatsUpdateSubscribe(movieSessionId);
  }

  Future _seatsUpdateSubscribe(String movieSessionId) async {
    _movieSessionId = movieSessionId;

    await _hubConnection
        .invoke('SubscribeToUpdateSeatsGroup', args: <Object>[movieSessionId]);
  }

  @override
  Future shoppingCartUpdateSubscribe(String shoppingCartId) async {
    _shoppingCartId = shoppingCartId;

    await _shoppingCartUpdateSubscribe(shoppingCartId);
  }

  Future _shoppingCartUpdateSubscribe(String shoppingCartId) async {
    await _hubConnection
        .invoke('RegisterShoppingCart', args: <Object>[shoppingCartId]);
  }

  Future<void> _tryStartHub() async {
    try {
      if (_hubConnection.state != HubConnectionState.Connected) {
        await _hubConnection.start();

        await _tryReconnect();


      }
    } on Exception catch (e) {
      _controller.add(DisconnectedEvent());
    }
  }

  Future _tryReconnect() async {
    print('tryReconnect');
    if (_movieSessionId != null) {
      if (_movieSessionId!.isNotEmpty) {
        await _seatsUpdateSubscribe(_movieSessionId!);
        print('seatsUpdateSubscribe reconnected');
      }
    }

    if (_shoppingCartId != null) {
      if (_shoppingCartId!.isNotEmpty) {
        await _shoppingCartUpdateSubscribe(_shoppingCartId!);
        print('shoppingCartUpdateSubscribe reconnected');
      }
    }

    print('tryReconnect reconnected');
  }
}
