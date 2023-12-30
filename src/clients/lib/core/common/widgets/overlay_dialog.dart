import 'package:flutter/material.dart';
import 'package:movie_theater_tickets/core/res/app_theme.dart';

import '../../res/app_styles.dart';

class OverlayDialog extends StatelessWidget {
  final Widget body;
  final Widget header;
  final double? width;
  final double? height;
  final double? headerHeight;
  final double? bodyHeight;

  const OverlayDialog(
      {super.key,
      required this.body,
      required this.header,
      this.width = 450,
      this.height = 250,
        this.headerHeight = 20,
        this.bodyHeight = 20});

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
        width: width,
        height: height,
        child: Column(
          children: [
             SizedBox(
              height: headerHeight,
            ),
            header,
             SizedBox(
              height: bodyHeight,
            ),
            body,
          ],
        ),
      ),
    );
  }
}
