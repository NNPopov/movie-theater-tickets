import 'package:flutter/material.dart';

import '../../../core/res/app_styles.dart';
import '../../about/presentation/views/shopping_cart_view.dart';
import '../../movies/presentation/views/movie_view.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

import '../../shopping_carts/presentation/views/shopping_cart_view.dart';
import 'menu_item_widget.dart';

class DashboardWidget extends StatefulWidget {
  const DashboardWidget({super.key, required this.route});

  final String route;

  @override
  State<DashboardWidget> createState() => _DashboardView();
}

class _DashboardView extends State<DashboardWidget> {
  @override
  void initState() {
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 40,
      padding: const EdgeInsets.only(left: 50, right: 50),
      decoration: const BoxDecoration(
        color: AppStyles.widgetColor,
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Expanded(
            child: MenuItemWidget(
                text: AppLocalizations.of(context)!.movies,
                route: widget.route,
                navigateId: MoviesView.id),
          ),
          Expanded(
            child: MenuItemWidget(
                text: 'About Us',
                route: widget.route,
                navigateId: AboutUsView.id),
          ),
          Expanded(
            child: MenuItemWidget(
                text: AppLocalizations.of(context)!.shopping_cart,
                route: widget.route,
                navigateId: ShoppingCartView.id),
          ),
        ],
      ),
    );
  }
}
