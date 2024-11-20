import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/core/res/app_theme.dart';
import '../../../../core/common/widgets/overlay_dialog.dart';
import '../../../../core/res/app_styles.dart';
import '../../../../core/utils/utils.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../helpers/constants.dart';
import '../../../hub/presentation/cubit/connectivity_bloc.dart';
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
        width: 290,
        alignment: Alignment.topLeft,
        decoration: BoxDecoration(
          color: Theme.of(context).widgetColor,
          borderRadius: BorderRadius.circular(AppStyles.defaultRadius),
          border: Border.all(
            color: Theme.of(context).defaultBorderColor,
            width: AppStyles.defaultBorderWidth,
          ),
        ),
        padding: const EdgeInsets.all(10.0),
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

        if (state.status == ShoppingCartStateStatus.initCreating) {
          _createShoppingCardDialog(context, state);
        }
        if (state.status == ShoppingCartStateStatus.createdCancel) {
          if (Navigator.of(_dialogContext).canPop()) {
            Navigator.of(_dialogContext).pop();
            dialogInitialized = false;
          }
        }

        if (state.status == ShoppingCartStateStatus.created) {
          if (dialogInitialized == true) {
            if (Navigator.of(_dialogContext).canPop()) {
              Navigator.of(_dialogContext).pop();
              dialogInitialized = false;
              context.read<ShoppingCartCubit>().getShoppingCartIfExits();
            }
          }
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

          return Column(children: [
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
                      color: Theme.of(context).widgetColor,
                      borderRadius:
                          BorderRadius.circular(AppStyles.defaultRadius),
                      border: Border.all(
                        color: rowSeat.isDirty == null || rowSeat.isDirty!
                            ? Colors.black26
                            : Colors.blue,
                        width: AppStyles.defaultBorderWidth,
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
                                      color:
                                          _getConditionColor(context, rowSeat)),
                                )),
                            IconButton(
                              icon: const Icon(Icons.delete),
                              color: _getConditionColor(context, rowSeat),
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

        return Container(
          width: 259,
          child: Column(
            children: [
              Text(AppLocalizations.of(context)!.shopping_cart),
              TextButton(
                onPressed: () {
                  onCreateShoppingCart();
                },
                child: Text(AppLocalizations.of(context)!
                    .select_desired_number_of_seats),
              ),
            ],
          ),
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

      double expirationValue =
          _calculateExpirationProgressBarValue(timeBeforeExpirationSeconds);

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

  double _calculateExpirationProgressBarValue(int timeBeforeExpirationSeconds) {
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
    return expirationValue;
  }

  Color _getConditionColor(BuildContext context, ShoppingCartSeat rowSeat) {
    if (Theme.of(context).brightness == Brightness.light) {
      return (rowSeat.isDirty == null || rowSeat.isDirty!)
          ? Colors.black26
          : Colors.black;
    } else {
      return (rowSeat.isDirty == null || rowSeat.isDirty!)
          ? Colors.white
          : Colors.white70;
    }
  }

  void onCreateShoppingCart() {
    context.read<ShoppingCartCubit>().initCreateShoppingCart();
  }

  @override
  void dispose() {
    super.dispose();
  }

  Future<void> onSeatUnselectPress(
      ShoppingCartSeat seat, String movieSessionId) async {
    var shoppingCartStatus = context.read<ShoppingCartCubit>().state.status;
    if (shoppingCartStatus != ShoppingCartStateStatus.initial &&
        shoppingCartStatus != ShoppingCartStateStatus.deleted) {
      await context.read<ShoppingCartCubit>().unSeatSelect(
          row: seat.seatRow!,
          seatNumber: seat.seatNumber!,
          movieSessionId: movieSessionId);
    }
  }

  void onCompletePurchase() async {
    await context.read<ShoppingCartCubit>().completePurchase();
  }

  late BuildContext _dialogContext;
  late bool dialogInitialized = false;

  void _createShoppingCardDialog(
      BuildContext context, ShoppingCartState state) {

    if (dialogInitialized != true) {
      showDialog(
          context: context,
          builder: (BuildContext dialogContext) {
            _dialogContext = dialogContext;
            return StatefulBuilder(
                builder: (BuildContext context, StateSetter setState) {
              dialogInitialized = true;
              return AlertDialog(
                backgroundColor: Theme.of(context).widgetColor,
                surfaceTintColor: Colors.black12,
                shape: RoundedRectangleBorder(
                  borderRadius: const BorderRadius.all(
                      Radius.circular(AppStyles.defaultRadius)),
                  side: BorderSide(
                      color: Theme.of(context).defaultBorderColor,
                      width: AppStyles.defaultBorderWidth),
                ),
                title: Text(AppLocalizations.of(context)!.shopping_cart),
                content: const Text(
                    'Select the number of seats you are going to buy'),
                actions: <Widget>[
                  BlocBuilder<ShoppingCartCubit, ShoppingCartState>(
                      builder: (context, state) {
                    return Column(children: [
                      TextFormField(
                        controller: _maxSeatsController,
                        decoration: InputDecoration(
                          hintText: 'Введите значение',
                          errorText: state.errorMessage,
                        ),
                      ),
                      TextButton(
                        onPressed: () => context
                            .read<ShoppingCartCubit>()
                            .createShoppingCartCancel(),
                        child: const Text('Cancel'),
                      ),
                      TextButton(
                        onPressed: () async {
                          var maxNumberOfSeats =
                              int.parse(_maxSeatsController.text);
                          await context
                              .read<ShoppingCartCubit>()
                              .createShoppingCart(maxNumberOfSeats);
                        },
                        child: const Text('Create a shopping card'),
                      ),
                    ]);
                  })
                ],
              );
            });
          });
    }

  }
}
