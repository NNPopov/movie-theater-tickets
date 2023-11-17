import 'package:flutter/material.dart';

import '../../../dashboards/presentation/dashboard_widget.dart';
import '../../../home/presentation/widgets/home_app_bar.dart';


class ShoppingCartView extends StatefulWidget {
  const ShoppingCartView( {super.key});

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
    return Scaffold(
        backgroundColor: Colors.white,
        appBar: const HomeAppBar(),
    body: Column(
    children: [

    const DashboardWidget(route: ShoppingCartView.id),]));}

}