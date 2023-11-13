import 'package:flutter/material.dart' hide Badge;
import '../../../auth/presentations/widgets/auth_widget.dart';
import '../../../globalisations_flutter/widgets/globalisation_widget.dart';
import '../../../shopping_carts/presentation/widgens/shopping_cart_icon_widget.dart';

class HomeAppBar extends StatelessWidget implements PreferredSizeWidget {
  const HomeAppBar({super.key});

  @override
  Widget build(BuildContext context) {
    return   AppBar(

            toolbarHeight: 60,
            // titleSpacing: 180,
      title: Container(height: 100,),
            flexibleSpace: Container(
              margin: const EdgeInsets.symmetric(vertical: 10, horizontal: 2),
              padding: const EdgeInsets.symmetric( horizontal: 2),
              height: 60,
              child: const Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Text(
                      "Come and Watch",
                      style:
                          TextStyle(fontSize: 25, fontWeight: FontWeight.bold),
                    ),
                  ),
                  GlobalisationWidget(),
                  ShoppingCartIconWidget(),
                  AuthWidget(),
                ],
              ),
            ),

            centerTitle: true,
            automaticallyImplyLeading: false,

        );
  }

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight);
}
