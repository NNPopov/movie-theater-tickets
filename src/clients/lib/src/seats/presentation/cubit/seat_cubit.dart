import 'dart:async';
import 'dart:convert';
import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:get_it/get_it.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../../../helpers/constants.dart';
import '../../../shopping_carts/domain/entities/seat.dart';
import '../../../shopping_carts/domain/entities/shopping_cart.dart';
import '../../../shopping_carts/domain/usecases/create_shopping_cart.dart';
import '../../../shopping_carts/presentation/cubit/shopping_cart_cubit.dart';
import '../../data/models/seat_dto.dart';
import '../../domain/entities/seat.dart';
import '../../domain/usecases/get_seats_by_movie_session_id.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';

part 'seat_state.dart';

GetIt getIt = GetIt.instance;

class SeatCubit extends Cubit<SeatState> {
  late List<Seat> _seats;
  late final StreamSubscription<ShoppingCartState> _shoppingCartStream;
  late final ShoppingCartCubit _shoppingCartCubit;
  final storage = const FlutterSecureStorage();
  late GetSeatsByMovieSessionId _getMovieSessionById;

  late HubConnection _hubConnection;



  SeatCubit(
      {GetSeatsByMovieSessionId? getMovieSessionById,
      required ShoppingCartCubit shoppingCartCubit})
      : _getMovieSessionById =
            getMovieSessionById ?? getIt.get<GetSeatsByMovieSessionId>(),
        _shoppingCartCubit = shoppingCartCubit,
        super(const InitialState()) {

    final httpConnectionOptions = HttpConnectionOptions(
        logMessageContent: true,
        requestTimeout: 50000,
    );

    var baseUrl = dotenv.env["BASE_API_URL"].toString();

    _hubConnection = HubConnectionBuilder().withUrl('${baseUrl}/cinema-hall-seats-hub',
     // _hubConnection = HubConnectionBuilder().withUrl('http://localhost:7628/cinema-hall-seats-hub',
         options: httpConnectionOptions)
         //.withHubProtocol(MessagePackHubProtocol())
         .withAutomaticReconnect()
        .build();
    _hubConnection.onreconnecting(({error}) {
      print("onreconnecting called");
      //connectionIsOpen = false;
    });



    // if (_hubConnection.state != HubConnectionState.Connected) {
    //    var start = _hubConnection.start();
    //   //connectionIsOpen = true;
    // }
    _hubConnection.on("SentState", _handleIncommingChatMessage);

    _shoppingCartStream = shoppingCartCubit.stream.listen((event) {



      if (event is ShoppingCartCurrentState ||
          event is ShoppingCartConflictState) {
        var selectingSeat = event as ShoppingCartCurrentState;

        updateSeatsState(selectingSeat.shoppingCard);
      }
    });
  }

  get movies => null;

  void updateSeatsState(ShoppingCart shoppingCard) {
    // if (shoppingCard.shoppingCartSeat != null &&
    //     shoppingCard.shoppingCartSeat.length >= 0) {
    //    _seats = _seats.map((e) {
    //     var currentSeats = shoppingCard.shoppingCartSeat
    //         .any((t) => e.row == t.seatRow && e.seatNumber == t.seatNumber);
    //
    //     return Seat.temp(
    //         row: e.row,
    //         seatNumber: e.seatNumber,
    //         blocked: currentSeats == true && e.initBlocked == false
    //             ? currentSeats
    //             : e.initBlocked,
    //         initBlocked: e.initBlocked);
    //   }).toList();
    // //  _seats = newSeat;
    //   emit(SeatsState(_seats));
    // }
  }

  void _handleIncommingChatMessage(List<Object?>? args) {

    var  senderName = args?[0];
    List<dynamic> movies = jsonDecode(jsonEncode(senderName));

    List<Seat> seatDtos =
    movies.map((json) => SeatDto.fromJson(json) as Seat).toList();



  emit(SeatsState(seatDtos));

}

  Future<void> getSeats(String movieSessionId) async {
    emit(const GettingSeats());
    if (_hubConnection.state != HubConnectionState.Connected) {
    await  _hubConnection.start();
      //connectionIsOpen = true;
    }
    _hubConnection.invoke("JoinGroup", args: <Object>[movieSessionId]);

    final result = await _getMovieSessionById(movieSessionId);
    result.fold((failure) => emit(SeatsError(failure.errorMessage)), (seats) {
      var shoppingCartState = _shoppingCartCubit.state;
      if (shoppingCartState is ShoppingCartCurrentState ||
          shoppingCartState is ShoppingCartConflictState) {
        var shoppingCartCurrentState =
            shoppingCartState as ShoppingCartCurrentState;

         _seats = seats.map((e) {
          var currentSeats = shoppingCartCurrentState
              .shoppingCard.shoppingCartSeat
              .any((t) => e.row == t.seatRow && e.seatNumber == t.seatNumber);

          return Seat.temp(
              row: e.row,
              seatNumber: e.seatNumber,
              blocked: currentSeats == true ? currentSeats : e.blocked,
              initBlocked:
                  currentSeats == true ? !currentSeats : e.initBlocked);
        }).toList();
       // _seats = newSeat;
      } else {
        _seats = seats;
      }
      emit(SeatsState(_seats));
    });
  }

  @override
  Future<void> close() {
    _shoppingCartStream.cancel();
    return super.close();
  }
}
