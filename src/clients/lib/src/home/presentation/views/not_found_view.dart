import 'package:auto_route/auto_route.dart';
import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/core/routing/app_router.gr.dart';
import 'package:movie_theater_tickets/l10n/gen/app_localizations.dart';

/// Catch-all 404 page shown for any unmatched route.
///
/// Replaces the legacy silent fallback to the Movies screen with an honest,
/// localized "page not found" message and a one-tap action back to Movies.
@RoutePage(name: 'NotFoundRoute')
class NotFoundView extends StatelessWidget {
  const NotFoundView({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            l10n.page_not_found_title,
            style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          Text(l10n.page_not_found_message),
          const SizedBox(height: 16),
          ElevatedButton(
            onPressed: () => context.router.replaceAll([const MoviesRoute()]),
            child: Text(l10n.back_to_home),
          ),
        ],
      ),
    );
  }
}
