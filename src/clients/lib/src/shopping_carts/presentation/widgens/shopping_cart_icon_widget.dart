import 'package:auto_route/auto_route.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:movie_theater_tickets/core/routing/app_router.gr.dart';
import '../cubit/shopping_cart_cubit.dart';
import 'package:movie_theater_tickets/l10n/gen/app_localizations.dart';

class ShoppingCartIconWidget extends StatefulWidget {
  const ShoppingCartIconWidget({super.key});

  @override
  State<ShoppingCartIconWidget> createState() => _ShoppingCartIconWidget();
}

class _ShoppingCartIconWidget extends State<ShoppingCartIconWidget> {
  @override
  void initState() {
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<ShoppingCartCubit, ShoppingCartState>(
      listener: (context, state) {},
      buildWhen: (context, state) {
        return true;
      },
      builder: (BuildContext context, ShoppingCartState state) {
        String countSelectedSeats = '0';

        if (state.status == ShoppingCartStateStatus.creating) {
          return const SizedBox(
            width: 70,
            height: 40,
            child: Column(
              children: [Text("0", style: TextStyle(fontSize: 12))],
            ),
          );
        }
        if (state.status != ShoppingCartStateStatus.initial &&
            state.status != ShoppingCartStateStatus.error) {
          countSelectedSeats =
              state.shoppingCart.shoppingCartSeat.length.toString() ?? '0';
        }

        return SizedBox(
          width: 70,
          height: 40,
          child: Row(
            children: [
              IconButton(
                icon: const Icon(Icons.shopping_cart),
                tooltip: AppLocalizations.of(context)!.shopping_cart,
                onPressed: () {
                  context.router.navigate(const ShoppingCartRoute());
                },
              ),
              Text(countSelectedSeats, style: const TextStyle(fontSize: 12)),
            ],
          ),
        );
      },
    );
  }

  @override
  void dispose() {
    super.dispose();
  }
}
