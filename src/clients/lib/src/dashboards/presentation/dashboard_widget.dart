import 'package:auto_route/auto_route.dart';
import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/core/res/app_theme.dart';
import 'package:movie_theater_tickets/core/routing/app_router.gr.dart';
import 'package:movie_theater_tickets/l10n/gen/app_localizations.dart';

import 'menu_item_widget.dart';

/// Persistent three-item menu, rendered once inside `HomeShell`.
///
/// Navigates via the router's `navigate` (replace active child) semantics and
/// derives the active-item highlight from the router's current child route, so
/// it stays correct after a switch.
class DashboardWidget extends StatefulWidget {
  const DashboardWidget({super.key});

  @override
  State<DashboardWidget> createState() => _DashboardView();
}

class _DashboardView extends State<DashboardWidget> {
  StackRouter? _router;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    final router = context.router;
    if (router != _router) {
      _router?.removeListener(_onRouteChanged);
      _router = router;
      _router!.addListener(_onRouteChanged);
    }
  }

  void _onRouteChanged() {
    if (mounted) setState(() {});
  }

  /// Switches the shell's content to [route] by replacing the active child,
  /// so tapping the active item is a no-op and bouncing keeps the stack
  /// bounded (depth 1) instead of pushing a new entry per tap.
  void _switchTo(BuildContext context, PageRouteInfo route) {
    context.router.replaceAll([route]);
  }

  @override
  void dispose() {
    _router?.removeListener(_onRouteChanged);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final activeName = context.router.topRoute.name;

    return Container(
      height: 40,
      padding: const EdgeInsets.only(left: 50, right: 50),
      decoration: BoxDecoration(color: Theme.of(context).widgetColor),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Expanded(
            child: MenuItemWidget(
              text: AppLocalizations.of(context)!.movies,
              isActive: activeName == MoviesRoute.name,
              onPressed: () => _switchTo(context, const MoviesRoute()),
            ),
          ),
          Expanded(
            child: MenuItemWidget(
              text: AppLocalizations.of(context)!.about_us,
              isActive: activeName == AboutRoute.name,
              onPressed: () => _switchTo(context, const AboutRoute()),
            ),
          ),
          Expanded(
            child: MenuItemWidget(
              text: AppLocalizations.of(context)!.shopping_cart,
              isActive: activeName == ShoppingCartRoute.name,
              onPressed: () => _switchTo(context, const ShoppingCartRoute()),
            ),
          ),
        ],
      ),
    );
  }
}
