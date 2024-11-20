import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/core/res/app_theme.dart';

import '../../../../core/res/app_styles.dart';
import '../../../../core/utils/utils.dart';
import '../../../auth/presentations/bloc/auth_cubit.dart';
import '../../../auth/presentations/bloc/auth_event.dart';
import '../../../auth/presentations/widgets/auth_safe_area_widget.dart';
import '../../../dashboards/presentation/dashboard_widget.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../domain/entities/shopping_cart.dart';
import '../cubit/shopping_cart_cubit.dart';

class ShoppingCartView extends StatefulWidget {
  const ShoppingCartView({super.key});

  static const id = '/shopping_cart_view';

  @override
  State<ShoppingCartView> createState() => _ShoppingCartView();
}

class _ShoppingCartView extends State<ShoppingCartView> {
  @override
  void initState() {
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return Column(children: [
      const DashboardWidget(route: ShoppingCartView.id),
      AuthSafeAreaWidget(
        authenticated: buildShoppingCart(),
        notAuthenticated: TextButton(
          onPressed: () {
            context.read<AuthBloc>().add(LogInEvent());
          },
          child: const Text('Please log in to continue'),
        ),
      ),
    ]);
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
      builder: (BuildContext context, ShoppingCartState state) {
        if (state.status == ShoppingCartStateStatus.creating) {
          return const Column(
            children: [Text("Shopping Cart")],
          );
        }
        if (state.status != ShoppingCartStateStatus.initial &&
            state.status != ShoppingCartStateStatus.error) {
          context.read<ShoppingCartCubit>().state;

          return Column(children: [
            Text(state.shoppingCart.status.toString()),
            const SizedBox(
              height: 30,
            ),
            ListView.builder(
                shrinkWrap: true,
                scrollDirection: Axis.vertical,
                itemCount: state.shoppingCart.shoppingCartSeat.length,
                itemBuilder: (context, rowIndex) {
                  var rowSeat = state.shoppingCart.shoppingCartSeat[rowIndex];
                  return Container(
                    width: 190,
                    height: 65,
                    alignment: Alignment.centerLeft,
                    decoration: BoxDecoration(
                      color: Theme.of(context).widgetColor,
                      borderRadius:
                          BorderRadius.circular(AppStyles.defaultRadius),
                      border: Border.all(
                        color: Colors.blue,
                        width: AppStyles.defaultBorderWidth,
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
                          onPressed: () async {
                            if (context
                                        .read<ShoppingCartCubit>()
                                        .state
                                        .status !=
                                    ShoppingCartStateStatus.initial &&
                                context
                                        .read<ShoppingCartCubit>()
                                        .state
                                        .status !=
                                    ShoppingCartStateStatus.deleted) {
                              await context
                                  .read<ShoppingCartCubit>()
                                  .unSeatSelect(
                                      row: rowSeat.seatRow!,
                                      seatNumber: rowSeat.seatNumber!,
                                      movieSessionId:
                                          state.shoppingCart.movieSessionId!);
                            }
                          },
                        ),
                      ],
                    ),
                  );
                }),
            if (state.status != ShoppingCartStateStatus.initial &&
                state.shoppingCart != null &&
                state.shoppingCart.priceCalculationResult != null &&
                state.shoppingCart.priceCalculationResult
                        ?.totalCartAmountAfterDiscounts !=
                    null)
              Container(
                width: 600,
                height: 70,
                child: Column(
                  children: [
                    Text(
                        'totalCartAmountBeforeDiscounts: ${state.shoppingCart!.priceCalculationResult!.totalCartAmountBeforeDiscounts.toString()}'),
                    Text(
                        'totalCartDiscounts: ${state.shoppingCart!.priceCalculationResult!.totalCartDiscounts.toString()}'),
                    Text(
                        'totalCartAmountAfterDiscounts: ${state.shoppingCart!.priceCalculationResult!.totalCartAmountAfterDiscounts.toString()}'),
                  ],
                ),
              ),
            if (state.shoppingCart.status == ShoppingCartStatus.InWork &&
                state.shoppingCart.shoppingCartSeat.isNotEmpty &&
                state.shoppingCart.isAssigned == true)
              TextButton(
                onPressed: () async {
                  await context.read<ShoppingCartCubit>().completePurchase();
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
                //       onCreateShoppingCart();
              },
              child: const Text('You have no selected places'),
            ),
          ],
        );
      },
    );
  }
}
