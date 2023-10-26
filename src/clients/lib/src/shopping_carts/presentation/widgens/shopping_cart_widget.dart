import 'package:flutter/material.dart';

import '../../../../core/utils/utils.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../cubit/shopping_cart_cubit.dart';

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
          var shoppingCardId = createdShoppingCard.shoppingCard.id;

          return SizedBox(
            width: 190,
            height: 200,
            child: Column(children: [
              Text("Shopping Cart:  ${shoppingCardId}"),
              ListView.builder(
                  shrinkWrap: true,
                  scrollDirection: Axis.vertical,
                  itemCount:
                      createdShoppingCard.shoppingCard.shoppingCartSeat.length,
                  itemBuilder: (context, rowIndex) {
                    var rowSeat = createdShoppingCard
                        .shoppingCard.shoppingCartSeat[rowIndex];
                    return SizedBox(
                        height: 22,
                        width: 200,
                        child: Text(
                            "Seat Row ${rowSeat.seatRow}, Number ${rowSeat.seatNumber}"));
                  }),
              if (createdShoppingCard.shoppingCard.shoppingCartSeat.isNotEmpty)
                TextButton(
                  onPressed: () {
                    onCreateShoppingCart();
                  },
                  child: const Text('Complete purchase'),
                ),
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
    );
  }

  void onCreateShoppingCart() {
    showDialog<String>(
        context: context,
        builder: (BuildContext context) => BlocProvider(
              create: (BuildContext context) => ShoppingCartCubit(),
              child: AlertDialog(
                title: const Text('Shopping cart'),
                content: const Text(
                    'Select the number of seats you are going to buy'),
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
                      Navigator.pop(context, _maxSeatsController.text);
                    },
                    child: const Text('Create a shopping card'),
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

  @override
  void dispose() {
    super.dispose();
  }
}
