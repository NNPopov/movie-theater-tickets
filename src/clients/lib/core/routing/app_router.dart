import 'package:auto_route/auto_route.dart';

import 'app_router.gr.dart';

/// Single typed route table for the app.
///
/// Replaces the legacy hand-rolled `generateRoute` (`core/services/router.main.dart`).
/// A persistent [HomeShell] hosts the five existing screens as child routes; an
/// unmatched path falls through to the catch-all [NotFoundRoute].
@AutoRouterConfig()
class AppRouter extends RootStackRouter {
  @override
  List<AutoRoute> get routes => [
    AutoRoute(
      page: HomeRoute.page,
      path: '/',
      children: [
        AutoRoute(page: MoviesRoute.page, path: ''),
        AutoRoute(page: AboutRoute.page, path: 'about'),
        AutoRoute(page: ShoppingCartRoute.page, path: 'cart'),
        AutoRoute(page: MovieSessionsRoute.page, path: 'sessions'),
        AutoRoute(page: SeatsRoute.page, path: 'seats'),
        AutoRoute(page: NotFoundRoute.page, path: '*'),
      ],
    ),
  ];
}
