import 'package:flutter/material.dart';

import '../../../../core/utils/utils.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../seats/domain/entities/seat.dart';
import '../../domain/entities/seat.dart';
import '../cubit/shopping_cart_cubit.dart';
import '../views/shopping_cart_view.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

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
        String countSelectedSeats = "0";

        if (state is CreatingShoppingCart) {
          return const SizedBox(
            width: 70,
            height: 40,
            child: Column(
              children: [
                Text(
                  "0",
                  style: TextStyle(fontSize: 12),
                )
              ],
            ),
          );
        }
        if (state is! ShoppingCartInitialState && state is! ShoppingCartError) {
          var createdShoppingCard = state as ShoppingCartCurrentState;
          var shoppingCardId = createdShoppingCard.shoppingCard.id;
          countSelectedSeats = createdShoppingCard
              .shoppingCard.shoppingCartSeat.length
              .toString() ?? "0";
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
                  Navigator.pushNamed(context, ShoppingCartView.id);
                }
            ),
            Text(
              countSelectedSeats,
              style: TextStyle(fontSize: 12),
            )
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
