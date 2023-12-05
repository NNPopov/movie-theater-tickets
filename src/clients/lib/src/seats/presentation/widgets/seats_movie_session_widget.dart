import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/src/seats/presentation/widgets/seat_widget.dart';
import '../../../../core/common/views/loading_view.dart';
import '../../../../core/common/views/no_data_view.dart';
import '../../../../core/errors/failures.dart';
import '../../../cinema_halls/domain/entity/cinema_hall_info.dart';
import '../../../cinema_halls/domain/entity/cinema_seat.dart';
import '../../../cinema_halls/presentation/cubit/movie_cubit.dart';
import '../../../movie_sessions/domain/entities/movie_session.dart';
import '../../../shopping_carts/presentation/cubit/shopping_cart_cubit.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../domain/entities/seat.dart';
import '../../domain/usecases/get_cinema_hall_info.dart';
import '../cubit/seat_cubit.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:dartz/dartz.dart' as t;
import 'package:get_it/get_it.dart';

final getIt = GetIt.instance;

class SeatsMovieSessionWidget extends StatefulWidget {
  const SeatsMovieSessionWidget(
      {super.key, required this.movieSession, required this.getCinemaHallInfo});

  final MovieSession movieSession;
  final GetCinemaHallInfo getCinemaHallInfo;

  @override
  State<SeatsMovieSessionWidget> createState() => _SeatsMovieSessionWidget();
}

class _SeatsMovieSessionWidget extends State<SeatsMovieSessionWidget> {
  @override
  void initState() {
    context
        .read<CinemaHallInfoCubit>()
        .getMovieById(widget.movieSession.cinemaHallId);
    super.initState();

    getSeats();
  }

  Future<void> getSeats() async {
    await context.read<SeatCubit>().getSeats(widget.movieSession.id);
  }

  @override
  void dispose() {
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return buildCinemaHallInfo(context);
  }

  Widget buildCinemaHallInfo(BuildContext context) {
    return BlocBuilder<CinemaHallInfoCubit, CinemaHallInfoState>(
        builder: (context, snapshot) {
      if (snapshot.status != CinemaHallInfoStatus.completed) {
        return const LoadingView();
      }
      return buildSeats(snapshot.movie.cinemaSeat, context);
    });
  }

  Widget buildSeats(List<List<CinemaSeat>> seats, BuildContext context) {
    var seatsWidth = seats[0].length * 19.0;
    var seatsHeight = seats.length * 22.0;
    try {
      return BlocSelector<ShoppingCartCubit, ShoppingCartState, String>(
          selector: (ShoppingCartState cart) {
        return cart.hashId;
      }, builder: (BuildContext context, String hashId) {
        return Container(
            height: seatsHeight + 110,
            width: seatsWidth + 110,
            padding: const EdgeInsets.all(20),
            alignment: Alignment.centerLeft,
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(10),
              border: Border.all(
                color: Colors.blue,
                width: 2,
              ),
            ),
            child: Column(children: [
              Row(
                children: [
                  const SizedBox(height: 40, width: 60),
                  Container(
                      height: 40,
                      width: seatsWidth,
                      alignment: Alignment.center,
                      child: Text(AppLocalizations.of(context)!.screen)),
                ],
              ),
              ListView.builder(
                  shrinkWrap: true,
                  scrollDirection: Axis.vertical,
                  itemCount: seats.length,
                  itemBuilder: (context, rowIndex) {
                    var rowSeats = seats[rowIndex];
                    return buildSeatBox(seatsWidth, context, rowSeats, hashId);
                  })
            ]));
      });
    } on Exception catch (e) {
      return SizedBox(
          height: 22,
          width: seatsWidth + 90,
          child: SeatWidget(
              text: '',
              backgroundColor: Colors.greenAccent,
              onPressed: () async {}));
    }
  }

  SizedBox buildSeatBox(double seatsWidth, BuildContext context,
      List<CinemaSeat> rowSeats, String hashId) {
    try {
      return SizedBox(
          height: 22,
          width: seatsWidth + 90,
          child: Row(children: [
            SizedBox(
                height: 19,
                width: 60,
                child: Text(
                    '${AppLocalizations.of(context)!.row}: ${rowSeats[0].row}')),
            ListView.builder(
                shrinkWrap: true,
                scrollDirection: Axis.horizontal,
                itemCount: rowSeats.length,
                itemBuilder: (context, index) {
                  var seatPlace = rowSeats[index];

                  return BlocSelector<SeatCubit, SeatState, Seat?>(
                    selector: (SeatState state) {
                      if (state.status != SeatStateStatus.loaded) {
                        return null;
                      }
                      if (state.seats.isEmpty) {
                        return null;
                      }
                      try {
                        var seat = state.seats.firstWhere((t) =>
                            t.seatNumber == seatPlace.seatNumber &&
                            t.row == seatPlace.row);

                        return seat;
                      }  catch (_) {
                        return null;
                      }

                    },
                    builder: (BuildContext context, Seat? state) {
                      if (state == null) {
                        return emptySeat(context);
                      }
                      return buildSeat(state, context, hashId);
                    },
                  );
                }),
          ]));
    } on Exception catch (e) {
      return SizedBox(
          height: 22,
          width: seatsWidth + 90,
          child: SeatWidget(
              text: '',
              backgroundColor: Colors.greenAccent,
              onPressed: () async {}));
    }
  }

  Widget emptySeat(BuildContext context) {
    return const SeatWidget(text: '', backgroundColor: Colors.black12);
  }

  Widget buildSeat(Seat seat, BuildContext context, String hashId) {
    print('buildSeat ${seat.row} ${seat.seatNumber}');
    try {
      if (seat.blocked &&
          seat.hashId == hashId &&
          seat.seatStatus == SeatStatus.selected) {
        return SeatWidget(
            text: seat.seatNumber.toString(),
            backgroundColor: Colors.greenAccent,
            onPressed: () async {
              await onSeatUnselectPress(seat);
            });
      }
      if (seat.blocked &&
          seat.hashId == hashId &&
          seat.seatStatus != SeatStatus.selected) {
        return SeatWidget(
            text: seat.seatNumber.toString(),
            backgroundColor: Colors.green,
            onPressed: () async {
              await onSeatUnselectPress(seat);
            });
      } else if (seat.blocked && seat.hashId != hashId) {
        return SeatWidget(
            text: seat.seatNumber.toString(),
            backgroundColor: Colors.blue,
            onPressed: () async {
              await onSeatUnselectPress(seat);
            });
      } else {
        return SeatWidget(
            text: seat.seatNumber.toString(),
            backgroundColor: Colors.grey,
            onPressed: () async {
              await onSelectSeatPress(seat);
            });
      }
    } on Exception catch (e) {
      return SeatWidget(
          text: '',
          backgroundColor: Colors.greenAccent,
          onPressed: () async {});
    }
  }

  Future<void> onSelectSeatPress(Seat seat) async {
    if (context.read<ShoppingCartCubit>().state.status !=
        ShoppingCartStateStatus.initial) {
      await context.read<ShoppingCartCubit>().seatSelect(
          row: seat.row,
          seatNumber: seat.seatNumber,
          movieSessionId: widget.movieSession.id);
    }
  }

  Future<void> onSeatUnselectPress(Seat seat) async {
    if (context.read<ShoppingCartCubit>().state.status !=
        ShoppingCartStateStatus.initial) {
      await context.read<ShoppingCartCubit>().unSeatSelect(
          row: seat.row,
          seatNumber: seat.seatNumber,
          movieSessionId: widget.movieSession.id);
    }
  }
}
