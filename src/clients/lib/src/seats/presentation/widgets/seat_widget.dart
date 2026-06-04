import 'package:flutter/material.dart';

import '../../../../core/res/app_styles.dart';

class SeatWidget extends StatelessWidget {
  const SeatWidget({
    super.key,
    this.foregroundColor = Colors.black,
    this.backgroundColor,
    this.onPressed,
    required this.text,
  });

  final String text;
  final Color? foregroundColor;
  final Color? backgroundColor;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 19,
      width: 19,
      child: GestureDetector(
        // Whole cell tappable, like the old button. A null [onPressed] renders
        // a non-interactive seat (sold / empty).
        behavior: HitTestBehavior.opaque,
        onTap: onPressed,
        child: DecoratedBox(
          decoration: BoxDecoration(color: backgroundColor),
          child: Center(
            child: Text(
              text,
              style: TextStyle(
                color: foregroundColor,
                fontSize: AppStyles.defaultFontSize,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
