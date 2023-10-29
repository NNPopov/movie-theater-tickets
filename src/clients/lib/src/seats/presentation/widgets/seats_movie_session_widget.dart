import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/core/extensions/context_extensions.dart';

import '../../../../core/common/views/loading_view.dart';
import '../../../../core/utils/utils.dart';
import '../../../movie_sessions/domain/entities/movie_session.dart';
import '../../../shopping_carts/presentation/cubit/shopping_cart_cubit.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../domain/entities/seat.dart';
import '../cubit/seat_cubit.dart';

class SeatsMovieSessionWidget extends StatefulWidget {
  const SeatsMovieSessionWidget({super.key, required this.movieSession});

  final MovieSession movieSession;

  @override
  State<SeatsMovieSessionWidget> createState() => _SeatsMovieSessionWidget();
}

class _SeatsMovieSessionWidget extends State<SeatsMovieSessionWidget> {
  @override
  void initState() {
    super.initState();
    //Future.microtask(() async {
      //await
      getSeats();
   // });
  }

  Future<void> getSeats() async {
    await context.read<SeatCubit>().getSeats(widget.movieSession.id);
  }

  @override
  void dispose() {
    super.dispose();
  }

  OverlayEntry? _overlayEntry;

  @override
  Widget build(BuildContext context) {

      return BlocConsumer<SeatCubit, SeatState>(
        listener: (context, state) {
          if (state is SeatsError) {
            Utils.showSnackBar(context, state.message,
                backgroundColor: Colors.red);
          }
        },
        buildWhen: (context, state) {
          if (state is SeatsError) {
            return false;
          } else {
            return true;
          }
        },
        builder: (context, state) {
          if (state is! SeatsState && state is! SeatsError) {
            return const LoadingView();
          }
          if ((state is SeatsState && state.seats.isEmpty) ||
              state is SeatsError) {
            return Center(
              child: Text(
                'No courses found\nPlease contact '
                    'admin or if you are admin, add courses',
                textAlign: TextAlign.center,
                style:
                context.theme.textTheme.headlineMedium?.copyWith(
                  fontWeight: FontWeight.w600,
                  color: Colors.grey.withOpacity(0.5),
                ),
              ),
            );
          }

          state as SeatsState;

          return BuildSeats(state.seats, context);
        },
      );
    }

    Widget BuildSeats(List<List<Seat>> seats, BuildContext context) {
      return Column(children: [
        SizedBox(height: 40, width: 100, child: Text('Screen')),
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
                        child: Text('Row: ${rowSeats[0].row}')),
                    ListView.builder(
                        shrinkWrap: true,
                        scrollDirection: Axis.horizontal,
                        itemCount: rowSeats.length,
                        itemBuilder: (context, index) {
                          var seat = rowSeats[index];
                          if (seat.blocked && seat.isCurrentReserve && seat.seatStatus == SeatStatus.selected) {
                            return SizedBox(
                                height: 19,
                                width: 19,
                                child: TextButton(
                                    style: ButtonStyle(
                                        padding: MaterialStateProperty.all(
                                            const EdgeInsets.symmetric(
                                                vertical: 2, horizontal: 2)),
                                        foregroundColor:
                                        MaterialStateProperty.all<Color>(
                                            Colors.black),
                                        backgroundColor:
                                        MaterialStateProperty.all<Color>(
                                            Colors.greenAccent)),
                                    onPressed: () async {
                                      await onSeatUnselectPress(seat);
                                    },
                                    child: Text(
                                      '${seat.seatNumber}',
                                      style: TextStyle(fontSize: 12),
                                    )));
                          } if (seat.blocked && seat.isCurrentReserve && seat.seatStatus != SeatStatus.selected) {
                            return SizedBox(
                                height: 19,
                                width: 19,
                                child: TextButton(
                                    style: ButtonStyle(
                                        padding: MaterialStateProperty.all(
                                            const EdgeInsets.symmetric(
                                                vertical: 2, horizontal: 2)),
                                        foregroundColor:
                                        MaterialStateProperty.all<Color>(
                                            Colors.black),
                                        backgroundColor:
                                        MaterialStateProperty.all<Color>(
                                            Colors.green)),
                                    onPressed: () async {
                                    },
                                    child: Text(
                                      '${seat.seatNumber}',
                                      style: TextStyle(fontSize: 12),
                                    )));
                          }else if (seat.blocked && !seat.isCurrentReserve) {
                            return SizedBox(
                                height: 19,
                                width: 19,
                                child: TextButton(
                                    style: ButtonStyle(
                                        padding: MaterialStateProperty.all(
                                            const EdgeInsets.symmetric(
                                                vertical: 2, horizontal: 2)),
                                        foregroundColor:
                                        MaterialStateProperty.all<Color>(
                                            Colors.black),
                                        backgroundColor:
                                        MaterialStateProperty.all<Color>(
                                            Colors.blue)),
                                    onPressed: () async {
                                      // test
                                      await onSeatUnselectPress(seat);
                                    },
                                    child: Text(
                                      '${seat.seatNumber}',
                                      style: TextStyle(fontSize: 12),
                                    )));
                          } else {
                            return SizedBox(
                                height: 19,
                                width: 19,
                                child: TextButton(
                                    style: ButtonStyle(
                                        padding: MaterialStateProperty.all(
                                            const EdgeInsets.symmetric(
                                                vertical: 2, horizontal: 2)),
                                        foregroundColor:
                                        MaterialStateProperty.all<Color>(
                                            Colors.black),
                                        backgroundColor:
                                        MaterialStateProperty.all<Color>(
                                            Colors.grey)),
                                    onPressed: () async {
                                      await onSelectSeatPress(seat);
                                    },
                                    child: Text('${seat.seatNumber}',
                                        style: TextStyle(fontSize: 12))));
                          }
                        })
                  ]));
            })
      ]);
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