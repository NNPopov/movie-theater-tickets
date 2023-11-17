import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/core/services/router.main.dart';
import '../../../../core/utils/utils.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../domain/entities/seat.dart';
import '../cubit/shopping_cart_cubit.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

class ShoppingCartWidget extends StatefulWidget {
  const ShoppingCartWidget({super.key});

  @override
  State<ShoppingCartWidget> createState() => _ShoppingCartWidget();
}

class _ShoppingCartWidget extends State<ShoppingCartWidget> {
  @override
  void initState() {
    super.initState();
  }

  final _maxSeatsController = TextEditingController();

  void createShoppingCard(BuildContext context) {
    var maxNumberOfSeats = int.parse(_maxSeatsController.text);
    context.read<ShoppingCartCubit>().createShoppingCart(maxNumberOfSeats);
    Navigator.pop(context, 'OK');
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<ShoppingCartCubit, ShoppingCartState>(
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
          return const SizedBox(
            width: 150,
            height: 200,
            child: Column(
              children: [Text("Shopping Cart")],
            ),
          );
        }
        if (state is! ShoppingCartInitialState && state is! ShoppingCartError) {
          context.read<ShoppingCartCubit>().state;

          var createdShoppingCard = state as ShoppingCartCurrentState;

          return Container(
            width: 220,
            height: 500,
            margin: const EdgeInsets.all(6.0),
            padding: const EdgeInsets.all(6.0),
            child: Column(children: [
              Text("${createdShoppingCard.shoppingCard.status.toString()}"),
              const SizedBox(
                height: 30,
              ),
              ListView.builder(
                  shrinkWrap: true,
                  scrollDirection: Axis.vertical,
                  itemCount:
                      createdShoppingCard.shoppingCard.shoppingCartSeat.length,
                  itemBuilder: (context, rowIndex) {
                    var rowSeat = createdShoppingCard
                        .shoppingCard.shoppingCartSeat[rowIndex];
                    return Container(
                      width: 190,
                      height: 65,
                      alignment: Alignment.centerLeft,
                      decoration: BoxDecoration(
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(
                          color: Colors.blue,
                          width: 2,
                        ),
                      ),
                      margin: const EdgeInsets.all(2.0),
                      padding: const EdgeInsets.all(10.0),
                      child: Row(
                        children: [
                          Container(
                              height: 40,
                              width: 140,
                              padding: const EdgeInsets.all(10.0),
                              child: Text(
                                  "${AppLocalizations.of(context)!.row} ${rowSeat.seatRow}, Number ${rowSeat.seatNumber}")),
                          IconButton(
                            icon: const Icon(Icons.delete),
                            tooltip: AppLocalizations.of(context)!.remove,
                            onPressed: () {
                              onSeatUnselectPress(
                                  rowSeat,
                                  createdShoppingCard
                                      .shoppingCard.movieSessionId!);
                            },
                          ),
                        ],
                      ),
                    );
                  }),
              if (createdShoppingCard
                      .shoppingCard.shoppingCartSeat.isNotEmpty &&
                  !createdShoppingCard.shoppingCard.isAssigned!)
                TextButton(
                  onPressed: () {
                    //  onAssignClient();
                  },
                  child: const Text('assignClient purchase'),
                ),
              if (createdShoppingCard
                      .shoppingCard.shoppingCartSeat.isNotEmpty &&
                  createdShoppingCard.shoppingCard.isAssigned!)
                TextButton(
                  onPressed: () {
                    onCompletePurchase();
                  },
                  child:  Text(AppLocalizations.of(context)!.complete_purchases),
                ),
            ]),
          );
        }

        return SizedBox(
          width: 150,
          height: 200,
          child: Column(
            children: [
              Text(AppLocalizations.of(context)!.shopping_cart),
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
    );
  }

  void onCreateShoppingCart() {
    if (context.read<ShoppingCartCubit>().state.shoppingCard.status != null) {
      return;
    }

    showDialog(
        context: context,
        builder: (BuildContext context) => BlocProvider(
              create: (BuildContext context) => ShoppingCartCubit(),
              child: BlocConsumer<ShoppingCartCubit, ShoppingCartState>(
                builder: (BuildContext context, ShoppingCartState state) {
                  return AlertDialog(
                    title: Text(AppLocalizations.of(context)!.shopping_cart),
                    content: const Text(
                        'Select the number of seats you are going to buy'),
                    actions: <Widget>[
                      TextFormField(
                        controller: _maxSeatsController,
                        keyboardType: TextInputType.phone,
                      ),
                      TextButton(
                        onPressed: () => Navigator.of(context).pop(),
                        child: const Text('Cancel'),
                      ),
                      TextButton(
                        onPressed: () async {
                          var maxNumberOfSeats =
                              int.parse(_maxSeatsController.text!);
                          await context
                              .read<ShoppingCartCubit>()
                              .createShoppingCart(maxNumberOfSeats);
                        },
                        child: const Text('Create a shopping card'),
                      ),
                    ],
                  );
                },
                listener: (BuildContext context, ShoppingCartState state) {
                  if (state is ShoppingCartCreatedState) {
                    log.info('ShoppingCartCreatedState received');

                    Navigator.of(context).pop();
                  }
                },
              ),
            )).then((valueFromDialog) async {
      await context.read<ShoppingCartCubit>().GetShoppingCartIfExits();
    });
  }

  @override
  void dispose() {
    super.dispose();
  }

  Future<void> onSeatUnselectPress(
      ShoppingCartSeat seat, String movieSessionId) async {
    if (context.read<ShoppingCartCubit>().state is! ShoppingCartInitialState) {
      await context.read<ShoppingCartCubit>().unSeatSelect(
          row: seat.seatRow!,
          seatNumber: seat.seatNumber!,
          movieSessionId: movieSessionId);
    }
  }

  void onCompletePurchase() async {
    await context.read<ShoppingCartCubit>().completePurchase();
  }
}
