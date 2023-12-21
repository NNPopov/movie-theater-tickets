import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/core/res/app_theme.dart';

import '../../res/app_styles.dart';

class OverlayDialog extends StatelessWidget {
  final Widget body;
  final Widget header;

  const OverlayDialog({super.key, required this.body, required this.header});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: Colors.grey.withOpacity(0.7),
      alignment: Alignment.center,
      child: Container(
        decoration: BoxDecoration(
          color: Theme.of(context).widgetColor,
          borderRadius: BorderRadius.circular(AppStyles.defaultRadius),
          border: Border.all(
            color: Theme.of(context).defaultBorderColor,
            width: AppStyles.defaultBorderWidth,
          ),
        ),
        padding: const EdgeInsets.all(20),
        width: 450,
        height: 250,
        child: Column(
          children: [
            const SizedBox(
              height: 20,
            ),
            header,
            const SizedBox(
              height: 20,
            ),
            body,

          ],
        ),
      ),
    );
  }
}
