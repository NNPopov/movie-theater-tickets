import 'package:auto_route/auto_route.dart';
import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/core/res/app_theme.dart';

import '../../dashboards/presentation/dashboard_widget.dart';
import 'widgets/home_app_bar.dart';

/// Persistent application shell.
///
/// Keeps the top app bar and the menu in place while the nested [AutoRouter]
/// swaps the content area between the five child screens. Replaces the legacy
/// `Scaffold(appBar: HomeAppBar, body: Navigator(onGenerateRoute: ...))` that
/// lived in `main.dart`.
@RoutePage(name: 'HomeRoute')
class HomeShell extends StatelessWidget {
  const HomeShell({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Theme.of(context).primaryBackgroundColor,
      appBar: const HomeAppBar(),
      body: const Column(
        children: [
          DashboardWidget(),
          Expanded(child: AutoRouter()),
        ],
      ),
    );
  }
}
