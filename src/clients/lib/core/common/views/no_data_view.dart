import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/core/extensions/context_extensions.dart';

class NoDataView extends StatelessWidget {
  const NoDataView({super.key});

  @override
  Widget build(BuildContext context) {
    return Material(
      type: MaterialType.transparency,
      child: Center(
        child: Text(
          'No data found\nPlease contact ',
          textAlign: TextAlign.center,
          style: context.theme.textTheme.headlineMedium?.copyWith(
            fontWeight: FontWeight.w600,
            color: Colors.grey.withOpacity(0.5),
          ),
        ),
      ),
    );
  }
}