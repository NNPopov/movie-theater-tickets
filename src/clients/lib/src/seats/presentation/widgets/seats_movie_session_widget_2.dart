import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/src/seats/presentation/widgets/seat_widget.dart';
import '../../../../core/common/views/loading_view.dart';
import '../../../../core/common/views/no_data_view.dart';
import '../../../../core/errors/failures.dart';
import '../../../movie_sessions/domain/entities/movie_session.dart';
import '../../../shopping_carts/presentation/cubit/shopping_cart_cubit.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../domain/entities/seat.dart';
import '../../domain/usecases/get_cinema_hall_info.dart';
import '../cubit/seat_cubit.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:dartz/dartz.dart' as t;

class SeatsMovieSessionWidgetTwo extends StatefulWidget {
  const SeatsMovieSessionWidgetTwo(
      {super.key, required this.movieSession, required this.getCinemaHallInfo});

  final MovieSession movieSession;
  final GetCinemaHallInfo getCinemaHallInfo;

  @override
  State<SeatsMovieSessionWidgetTwo> createState() =>
      _SeatsMovieSessionWidgetTwo();
}

class _SeatsMovieSessionWidgetTwo extends State<SeatsMovieSessionWidgetTwo> {
  @override
  void initState() {
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
    return FutureBuilder<t.Either<Failure, List<List<Seat>>>>(
        future: widget.getCinemaHallInfo(widget.movieSession.id),
        initialData: null,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting ||
              snapshot.data == null) {
            return const LoadingView();
          }

          return snapshot.data!.fold((l) {
            return NoDataView();
          }, (seatst) {
            if (seatst != null) {
              return BuildSeats(seatst!, context);
            } else {
              return NoDataView();
            }
          });
        });
  }

  Widget BuildSeats(List<List<Seat>> seats, BuildContext context) {
    var seatsWidth = seats[0].length * 19.0;
    var seatsHeight = seats.length * 22.0;

    return BlocSelector<ShoppingCartCubit, ShoppingCartState, String>(
        selector: (ShoppingCartState cart) {
      return cart.hashId;
    }, builder: (BuildContext context, String hashId) {
      return Container(
          height: seatsHeight + 110,
          width: seatsWidth + 110,
          padding: EdgeInsets.all(20),
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
                SizedBox(height: 40, width: 60),
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
                  return SizedBox(
                      height: 22,
                      width: 600,
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
                                  if (state.seats.isEmpty) {
                                    return null;
                                  }

                                  var seat = state.seats.firstWhere((t) =>
                                      t.seatNumber == seatPlace.seatNumber &&
                                      t.row == seatPlace.row);
                                  return seat;
                                },
                                builder: (BuildContext context, Seat? state) {
                                  if (state == null) {
                                    return EmptySeat(context);
                                  }
                                  return BuildSeat(state, context, hashId);
                                },
                              );
                            })
                      ]));
                })
          ]));
    });
  }

  Widget EmptySeat(BuildContext context) {
    return const SeatWidget(text: '', backgroundColor: Colors.white);
  }

  Widget BuildSeat(Seat seat, BuildContext context, String hashId) {
    if (seat.blocked &&
        seat.hashId==hashId &&
        seat.seatStatus == SeatStatus.selected) {
      return SeatWidget(
          text: seat.seatNumber.toString(),
          backgroundColor: Colors.greenAccent,
          onPressed: () async {
            await onSeatUnselectPress(seat);
          });
    }
    if (seat.blocked &&
        seat.hashId==hashId &&
        seat.seatStatus != SeatStatus.selected) {
      return SeatWidget(
          text: seat.seatNumber.toString(),
          backgroundColor: Colors.green,
          onPressed: () async {
            await onSeatUnselectPress(seat);
            ;
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
  }

  Future<void> onSelectSeatPress(Seat seat) async {
    if (context.read<ShoppingCartCubit>().state is! ShoppingCartInitialState) {
      await context.read<ShoppingCartCubit>().seatSelect(
          row: seat.row,
          seatNumber: seat.seatNumber,
          movieSessionId: widget.movieSession.id);
    }
  }

  Future<void> onSeatUnselectPress(Seat seat) async {
    if (context.read<ShoppingCartCubit>().state is! ShoppingCartInitialState) {
      await context.read<ShoppingCartCubit>().unSeatSelect(
          row: seat.row,
          seatNumber: seat.seatNumber,
          movieSessionId: widget.movieSession.id);
    }
  }
}
