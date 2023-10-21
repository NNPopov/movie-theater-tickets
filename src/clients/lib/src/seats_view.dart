import 'package:flutter/material.dart';
import 'package:carousel_slider/carousel_slider.dart';
import 'package:movie_theater_tickets/core/extensions/context_extensions.dart';
import 'package:movie_theater_tickets/src/seats/domain/entities/seat.dart';
import 'package:movie_theater_tickets/src/seats/presentation/cubit/seat_cubit.dart';
import 'package:movie_theater_tickets/src/shopping_carts/presentation/cubit/shopping_cart_cubit.dart';
import '../core/common/views/loading_view.dart';
import '../core/utils/utils.dart';
import 'movie_sessions/domain/entities/movie_session.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:get_it/get_it.dart';
import 'package:collection/collection.dart';

GetIt getIt = GetIt.instance;

class SeatsView extends StatefulWidget {
  const SeatsView(this.movieSession, {super.key});

  final MovieSession movieSession;
  static const id = '/seats';

  @override
  State<StatefulWidget> createState() => _SeatsView();
}

class _SeatsView extends State<SeatsView> {
  CarouselController buttonCarouselController = CarouselController();

  final _maxSeatsController = TextEditingController();

  void getSeats() {
    context.read<SeatCubit>().getSeats(widget.movieSession.id);
  }

  void createShoppingCard(BuildContext context) {
    var maxNumberOfSeats = int.parse(_maxSeatsController.text);
    context.read<ShoppingCartCubit>().createShoppingCart(maxNumberOfSeats);
    Navigator.pop(context, 'OK');
  }

  @override
  void initState() {
    super.initState();
    getSeats();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
        backgroundColor: Colors.white,
        appBar: AppBar(title: Text("seats")),
        body: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: BlocConsumer<SeatCubit, SeatState>(
                listener: (context, state) {
                  if (state is SeatsError) {
                    Utils.showSnackBar(context, state.message);
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
                        style: context.theme.textTheme.headlineMedium?.copyWith(
                          fontWeight: FontWeight.w600,
                          color: Colors.grey.withOpacity(0.5),
                        ),
                      ),
                    );
                  }

                  state as SeatsState;

                  final seats = groupBy(state.seats, (seat) => seat.row)
                      .values
                      .map((seats) => seats.toList()
                        ..sort((a, b) => a.seatNumber - b.seatNumber))
                      .toList();

                  return BuildSeats(seats, context);
                },
              ),
            ),
            BlocConsumer<ShoppingCartCubit, ShoppingCartState>(
              listener: (context, state) {
                if (state is ShoppingCartError) {
                  Utils.showSnackBar(context, state.message);
                }
                if (state is ShoppingCartConflictState) {
                  Utils.showSnackBar(context, 'This place is already occupied');
                }
              },
              buildWhen: (context, state) {
                if (state is ShoppingCartError) {
                  return false;
                } else {
                  return true;
                }
              },
              builder: (BuildContext context, ShoppingCartState state) {

                if (state is CreatingShoppingCart) {
                  return SizedBox(
                    width: 150,
                    height: 200,
                    child: Column(
                      children: [const Text("Shopping Cart")],
                    ),
                  );
                }
                if (state is! ShoppingCartInitialState && state is! ShoppingCartError) {
                  context.read<ShoppingCartCubit>().state;

                  var createdShoppingCard = state as ShoppingCartCurrentState;
                  var shoppingCardId = createdShoppingCard.shoppingCard.id;

                  return SizedBox(
                    width: 190,
                    height: 200,
                    child: Column(children: [
                      Text("Shopping Cart:  ${shoppingCardId}"),
                      ListView.builder(
                          shrinkWrap: true,
                          scrollDirection: Axis.vertical,
                          itemCount: createdShoppingCard
                              .shoppingCard.shoppingCartSeat.length,
                          itemBuilder: (context, rowIndex) {
                            var rowSeat = createdShoppingCard
                                .shoppingCard.shoppingCartSeat[rowIndex];
                            return SizedBox(
                                height: 22,
                                width: 200,
                                child: Text(
                                    "Seat Row ${rowSeat.seatRow}, Number ${rowSeat.seatNumber}"));
                          })
                    ]),
                  );
                }


                return SizedBox(
                  width: 150,
                  height: 200,
                  child: Column(
                    children: [
                      const Text("Shopping Cart"),
                      TextButton(
                        onPressed: () {
                          onCreateShoppingCart();
                        },
                        child: const Text('Select seats'),
                      ),
                    ],
                  ),
                );
              },
            )
          ],
        ));
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
                // color: Colors
                //     .primaries[seat.row % Colors.primaries.length],
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
                        if (seat.initBlocked) {
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
                                    await onSeatPress(seat);
                                    },
                                  child: Text(
                                    '${seat.seatNumber}',
                                    style: TextStyle(fontSize: 12),
                                  )));
                        } else if (seat.blocked) {
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
                                    await onSeatPress(seat);
                                  },
                                  child: Text('${seat.seatNumber}',
                                      style: TextStyle(fontSize: 12))));
                        }
                      })
                ]));
          })
    ]);
  }

  @override
  void dispose() {
    super.dispose();
  }

  void onCreateShoppingCart() {
    showDialog<String>(
        context: context,
        builder: (BuildContext context) => BlocProvider(
              create: (BuildContext context) => ShoppingCartCubit(),
              child: AlertDialog(
                title: const Text('AlertDialog Title'),
                content: const Text('AlertDialog description'),
                actions: <Widget>[
                  TextFormField(
                    controller: _maxSeatsController,
                    keyboardType: TextInputType.phone,
                  ),
                  TextButton(
                    onPressed: () => Navigator.pop(context, 'Cancel'),
                    child: const Text('Cancel'),
                  ),
                  TextButton(
                    onPressed: () {
                      //
                      Navigator.pop(context, _maxSeatsController.text
                          //'OK'
                          );

                      // pressSeat(seat);
                    },
                    child: const Text('OK'),
                  ),
                ],
              ),
            )).then((valueFromDialog) {
      if (valueFromDialog != "Cancel") {
        var maxNumberOfSeats = int.parse(valueFromDialog!);
        context.read<ShoppingCartCubit>().createShoppingCart(maxNumberOfSeats);
      }
    });
  }

  Future<void> onSeatPress(Seat seat) async {
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
