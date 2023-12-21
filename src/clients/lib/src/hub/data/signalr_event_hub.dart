import 'dart:async';
import 'dart:convert';

import '../../../core/common/app_logger.dart';
import '../../helpers/constants.dart';
import '../../seats/data/models/seat_dto.dart';
import '../../seats/domain/entities/seat.dart';
import '../../seats/domain/usecases/update_seats_sate_usecase.dart';
import '../../server_state/data/models/server_state_dto.dart';
import '../../server_state/domain/entities/server_state.dart';
import '../../server_state/domain/usecases/update_server_state_usecase.dart';
import '../../shopping_carts/data/models/seat_info_dto.dart';
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
      required this.updateSeatsState,
      required this.updateServerStateUseCase});

  final logger = getLogger(SignalREventHub);

  late HubConnection _hubConnection;
  late ShoppingCartUpdateStateUseCase updateShoppingCartState;
  late UpdateSeatsStateUseCase updateSeatsState;
  late UpdateServerStateUseCase updateServerStateUseCase;

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
      if (_hubConnection.state != HubConnectionState.Connected) {
        await _hubConnection.stop();
      }
    } catch (e) {
      logger.e("hubConnection was disconnected with error", error: e);
    }

    Logger.root.level = Level.ALL;
    Logger.root.onRecord.listen((LogRecord rec) {
      logger.d('${rec.level.name}: ${rec.time}: ${rec.message}');
    });

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

      logger.d("Connected called");
    });

    _hubConnection.onreconnecting(({error}) {
      _controller.add(ReconnectingEvent());
      logger.i("Reconnecting called");
    });

    _hubConnection.onclose(({error}) {
      _controller.add(DisconnectedEvent());
      logger.e("On Close called", error: error);
    });

    _hubConnection.on('SentShoppingCartState', _shoppingCartStateUpdate);

    _hubConnection.on('SentCinemaHallSeatsState', _seatsStateUpdate);

    _hubConnection.on('SentServerState', _sentServerState);

    try {
      await _tryStartHub();
      _controller.add(ConnectedEvent());
    } on Exception {
      _controller.add(DisconnectedEvent());
    }
  }

  Future<void> _shoppingCartStateUpdate(List<Object?>? args) async {
    try {
      logger.d('shoppingCartStateUpdate received');
      var senderName = args?[0];
      var movies = jsonDecode(jsonEncode(senderName));

      ShoppingCart shoppingCartValue = ShoppingCartDto.fromJson(movies);

      await updateShoppingCartState(shoppingCartValue);
    } on Exception catch (e) {
      logger.e('Unable process shoppingCartStateUpdate  $args', error: e);
    }
  }

  Future<void> _seatsStateUpdate(List<Object?>? args) async {
    logger.d('seatsStateUpdate received');

    List<dynamic> movies = jsonDecode(jsonEncode(args?[0]));

    List<Seat> seatsDto = movies.map((json) => SeatDto.fromJson(json)).toList();

    await updateSeatsState(seatsDto);
  }

  Future<void> _sentServerState(List<Object?>? args) async {
    logger.d('sentServerState received');

    var movies = jsonDecode(jsonEncode(args?[0]));

    ServerState serverState =
        ServerStateDto.fromJson(movies as Map<String, dynamic>);

    await updateServerStateUseCase(serverState);
  }

  @override
  Future unsubscribe() async {
    logger.i('unsubscribe');
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
  Future seatSelect(SeatInfoDto seatSelectRequestDto) async {
    await _hubConnection.invoke('SeatSelect', args: <Object>[
      seatSelectRequestDto.shoppingCartId,
      seatSelectRequestDto.row,
      seatSelectRequestDto.number,
      seatSelectRequestDto.showtimeId
    ]);
  }

  @override
  Future seatUnselect(SeatInfoDto seatSelectRequestDto) async {
    await _hubConnection.invoke('SeatUnselect', args: <Object>[
      seatSelectRequestDto.shoppingCartId,
      seatSelectRequestDto.row,
      seatSelectRequestDto.number,
      seatSelectRequestDto.showtimeId
    ]);
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
    } on Exception {
      _controller.add(DisconnectedEvent());
    }
  }

  Future _tryReconnect() async {
    logger.d('tryReconnect');

    try {
      if (_movieSessionId != null) {
        if (_movieSessionId!.isNotEmpty) {
          await _seatsUpdateSubscribe(_movieSessionId!);
          logger.d('seatsUpdateSubscribe reconnected');
        }
      }
    } on Exception catch (e) {
      logger.e('Unable resubscribe movieSessionId', error: e);
    }
    try {
      if (_shoppingCartId != null) {
        if (_shoppingCartId!.isNotEmpty) {
          await _shoppingCartUpdateSubscribe(_shoppingCartId!);
          logger.d('shoppingCartUpdateSubscribe reconnected');
        }
      }
    } on Exception catch (e) {
      logger.e('Unable resubscribe shoppingCartId', error: e);
    }

  }

  @override
  Future<void> shoppingCartRemoveSubscribe(String shoppingCartId) async {
    _shoppingCartId = null;
    logger.d('shoppingCartRemoveSubscribe');
    await _hubConnection
        .invoke('UnsubscribeShoppingCart', args: <Object>[shoppingCartId]);
  }
}
