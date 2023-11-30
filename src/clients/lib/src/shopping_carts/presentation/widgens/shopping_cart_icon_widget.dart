import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../cubit/shopping_cart_cubit.dart';
import '../views/shopping_cart_view.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';




class ShoppingCartIconWidget extends StatefulWidget {
  const ShoppingCartIconWidget(this.navigatorKey,{super.key });

 final  GlobalKey<NavigatorState> navigatorKey;
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
              children: [
                Text(
                  "0",
                  style: TextStyle(fontSize: 12),
                )
              ],
            ),
          );
        }
        if (state.status != ShoppingCartStateStatus.initial && state.status != ShoppingCartStateStatus.error) {

          countSelectedSeats = state
              .shoppingCart.shoppingCartSeat.length
              .toString() ?? '0';
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
                widget.navigatorKey.currentState?.pushNamed(ShoppingCartView.id);
                //  Navigator.pushNamed(context, ShoppingCartView.id);
                }
            ),
            Text(
              countSelectedSeats,
              style: const TextStyle(fontSize: 12),
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
