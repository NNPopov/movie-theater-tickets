import 'package:flutter/material.dart';

//import 'package:movie_theater_tickets/core/services/router.main.dart';
import '../../../../core/utils/utils.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../auth/presentations/bloc/auth_cubit.dart';
import '../../../auth/presentations/bloc/auth_event.dart';
import '../../../auth/presentations/widgets/auth_safe_area_widget.dart';
import '../../../helpers/constants.dart';
import '../../../server_state/domain/entities/server_state.dart';
import '../../../server_state/presentation/cubit/server_state_cubit.dart';
import '../../domain/entities/seat.dart';
import '../../domain/entities/shopping_cart.dart';
import '../cubit/shopping_cart_cubit.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:get_it/get_it.dart';

import '../views/shopping_cart_view.dart';

final getIt = GetIt.instance;

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
    return Container(
        width: 260,
        height: 500,
        alignment: Alignment.topLeft,
        //margin: const EdgeInsets.only(top: 6, left: 10, right: 40.0),
        padding: const EdgeInsets.all(6.0),
        child: buildShoppingCart());
  }

  BlocConsumer<ShoppingCartCubit, ShoppingCartState> buildShoppingCart() {
    return BlocConsumer<ShoppingCartCubit, ShoppingCartState>(
      listener: (context, state) {
        if (state.status == ShoppingCartStateStatus.error ||
            state.status == ShoppingCartStateStatus.createValidationError) {
          Utils.showSnackBar(context, state.errorMessage ?? '');
        }

        if (state.status == ShoppingCartStateStatus.deleted) {
          Utils.showSnackBar(context, 'Shopping cart was expired or deleted');
        }
      },
      buildWhen: (context, state) {
        if (state.status == ShoppingCartStateStatus.error) {
          return false;
        } else {
          return true;
        }
      },
      builder: (BuildContext context, ShoppingCartState shoppingCartState) {
        if (shoppingCartState.status == ShoppingCartStateStatus.creating) {
          return const Column(
            children: [Text("Shopping Cart")],
          );
        }
        if (shoppingCartState.status != ShoppingCartStateStatus.initial &&
            shoppingCartState.status != ShoppingCartStateStatus.error) {
          context.read<ShoppingCartCubit>().state;

          return Column(

              children: [
            Text(shoppingCartState.shoppingCart.status.toString()),
            const SizedBox(
              height: 30,
            ),
            ListView.builder(
                shrinkWrap: true,
                scrollDirection: Axis.vertical,
                itemCount:
                    shoppingCartState.shoppingCart.shoppingCartSeat.length,
                itemBuilder: (context, rowIndex) {
                  var rowSeat =
                      shoppingCartState.shoppingCart.shoppingCartSeat[rowIndex];
                  return Container(
                    width: 255,
                    height: 70,
                    alignment: Alignment.centerLeft,
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(
                        color: rowSeat.isDirty == null || rowSeat.isDirty!
                            ? Colors.black26
                            : Colors.blue,
                        width: 2,
                      ),
                    ),
                    margin: const EdgeInsets.all(2.0),
                    padding: const EdgeInsets.all(10.0),
                    child: Column(
                      children: [
                        Row(
                          children: [
                            Container(
                                height: 40,
                                width: 125,
                                padding: EdgeInsets.symmetric(
                                    vertical: 10, horizontal: 0),
                                child: Text(
                                    "${AppLocalizations.of(context)!.row} ${rowSeat.seatRow}, Number ${rowSeat.seatNumber}")),
                            Container(
                                height: 40,
                                width: 25,
                                padding: EdgeInsets.symmetric(
                                    vertical: 10, horizontal: 0),
                                child: Text(
                                  "${rowSeat.price}€",
                                  style: TextStyle(
                                      color: rowSeat.isDirty == null ||
                                              rowSeat.isDirty!
                                          ? Colors.black26
                                          : Colors.black),
                                )),
                            IconButton(
                              icon: const Icon(Icons.delete),
                              color: rowSeat.isDirty == null || rowSeat.isDirty!
                                  ? Colors.black26
                                  : Colors.black,
                              tooltip: AppLocalizations.of(context)!.remove,
                              onPressed: () {
                                onSeatUnselectPress(
                                    rowSeat,
                                    shoppingCartState
                                        .shoppingCart.movieSessionId!);
                              },
                            ),
                          ],
                        ),
                        if (shoppingCartState.shoppingCart.status ==
                            ShoppingCartStatus.InWork)
                          expirationProgressBar(rowSeat),
                      ],
                    ),
                  );
                }),
            if (shoppingCartState.shoppingCart.shoppingCartSeat.isNotEmpty)
              TextButton(
                onPressed: () {
                  Navigator.pushNamed(context, ShoppingCartView.id);
                },
                child: Text(AppLocalizations.of(context)!.complete_purchases),
              ),
          ]);
        }

        return Column(
          children: [
            Text(AppLocalizations.of(context)!.shopping_cart),
            TextButton(
              onPressed: () {
                onCreateShoppingCart();
              },
              child: const Text('Select seats'),
            ),
          ],
        );
      },
    );
  }

  Widget expirationProgressBar(ShoppingCartSeat rowSeat) {
    return BlocBuilder<ServerStateCubit, ServerState>(
        builder: (BuildContext context, ServerState state) {
      return seatExpirationProgressBar(rowSeat, state);
    });
  }

  Container seatExpirationProgressBar(
      ShoppingCartSeat rowSeat, ServerState state) {
    if (rowSeat.selectionExpirationTime != null &&
        state != ServerState.initState()) {
      Duration timeBeforeExpiration =
          rowSeat.selectionExpirationTime!.difference(state.serverDateTime);
      int timeBeforeExpirationSeconds = timeBeforeExpiration.inSeconds;

      double expirationValue = 0;

      if (timeBeforeExpirationSeconds < Constants.SEAT_EXPIRATION_SEC + 100) {
        var expirationPercentage =
            timeBeforeExpirationSeconds / Constants.SEAT_EXPIRATION_SEC;

        if (expirationPercentage > 0.9) {
          expirationValue = 100;
        } else if (expirationPercentage > 0.8) {
          expirationValue = 90;
        } else if (expirationPercentage > 0.7) {
          expirationValue = 80;
        } else if (expirationPercentage > 0.6) {
          expirationValue = 70;
        } else if (expirationPercentage > 0.5) {
          expirationValue = 60;
        } else if (expirationPercentage > 0.4) {
          expirationValue = 50;
        } else if (expirationPercentage > 0.3) {
          expirationValue = 40;
        } else if (expirationPercentage > 0.2) {
          expirationValue = 30;
        } else if (expirationPercentage > 0.1) {
          expirationValue = 20;
        } else {
          expirationValue = 10;
        }
      }
      var containerColour = expirationValue <= 20
          ? Colors.red
          : expirationValue <= 60
              ? Colors.blue
              : Colors.green;

      return Container(
        alignment: Alignment.centerLeft,
        width: 220,
        child: Container(
          alignment: Alignment.centerLeft,
          height: 3,
          width: expirationValue * 1.9,
          padding: const EdgeInsets.all(0.0),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(1),
            color: containerColour,
            border: Border.all(
              color: containerColour,
              width: 2,
            ),
          ),
        ),
      );
    }

    return Container(
      height: 3,
      width: 30,
      padding: const EdgeInsets.all(2.0),
    );
  }

  void onCreateShoppingCart() {
    if (context.read<ShoppingCartCubit>().state.shoppingCart.status != null) {
      return;
    }

    showDialog(
      context: context,
      builder: (BuildContext context) =>
          BlocConsumer<ShoppingCartCubit, ShoppingCartState>(
        builder: (BuildContext context, ShoppingCartState state) {
          return AlertDialog(
            title: Text(AppLocalizations.of(context)!.shopping_cart),
            content:
                const Text('Select the number of seats you are going to buy'),
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
                  var maxNumberOfSeats = int.parse(_maxSeatsController.text);
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
          if (state.status == ShoppingCartStateStatus.created) {
            Navigator.of(context).pop();
          }
        },
      ),
      //),
    ).then((valueFromDialog) async {
      await context.read<ShoppingCartCubit>().getShoppingCartIfExits();
    });
  }

  @override
  void dispose() {
    super.dispose();
  }

  Future<void> onSeatUnselectPress(
      ShoppingCartSeat seat, String movieSessionId) async {
    if (context.read<ShoppingCartCubit>().state.status !=
            ShoppingCartStateStatus.initial &&
        context.read<ShoppingCartCubit>().state.status !=
            ShoppingCartStateStatus.deleted) {
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
