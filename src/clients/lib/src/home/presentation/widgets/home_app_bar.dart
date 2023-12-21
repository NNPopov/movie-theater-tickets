import 'package:flutter/material.dart' hide Badge;
import '../../../auth/presentations/widgets/auth_widget.dart';
import '../../../globalisations_flutter/widgets/globalisation_widget.dart';
import '../../../shopping_carts/presentation/widgens/shopping_cart_icon_widget.dart';
import '../../../theme_flutter/widgets/theme_widget.dart';

class HomeAppBar extends StatelessWidget implements PreferredSizeWidget {
  const HomeAppBar( this.navigatorKey, {super.key, });
  final GlobalKey<NavigatorState> navigatorKey;

  @override
  Widget build(BuildContext context) {

    double width = MediaQuery.of(context).size.width;

    if(width > 500)
      {
    return   AppBar(

            toolbarHeight: 60,
            // titleSpacing: 180,
      title: Container(height: 100,),
            flexibleSpace: Container(
              margin: const EdgeInsets.symmetric(vertical: 10, horizontal: 2),
              padding: const EdgeInsets.symmetric( horizontal: 2),
              height: 60,
              child:  Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const SizedBox(width: 20),
                  buildTitle(),
                  const GlobalisationWidget(),
                  ShoppingCartIconWidget(navigatorKey),
                  ThemeWidget(),
                  const AuthWidget(),
                ],
              ),
            ),

            centerTitle: true,
            automaticallyImplyLeading: false,

        );}

    else
      {
        return   AppBar(

          toolbarHeight: 90,
          // titleSpacing: 180,
          title: Container( child: Row(
            children: [
              buildTitle(),
              ShoppingCartIconWidget(navigatorKey),
            ],
          )),
          flexibleSpace: Container(
           // margin: const EdgeInsets.symmetric(vertical: 10, horizontal: 2),
            padding: const EdgeInsets.only( left: 5),
            height: 100,
            child:  Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const GlobalisationWidget(),

                const AuthWidget(),
              ],
            ),
          ),

          centerTitle: true,
          automaticallyImplyLeading: true,

        );
      }


  }

  Expanded buildTitle() {
    return const Expanded(
                  child: Text(
                    "Come and Watch",
                    style:
                        TextStyle(fontSize: 25, fontWeight: FontWeight.bold),
                  ),
                );
  }

  @override
  Size get preferredSize => const Size.fromHeight(kToolbarHeight);
}
