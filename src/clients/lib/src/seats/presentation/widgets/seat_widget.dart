import 'package:flutter/material.dart';

import '../../../../core/res/app_styles.dart';

class SeatWidget extends StatelessWidget {
  const SeatWidget(
      {super.key,
      this.foregroundColor=Colors.black,
      this.backgroundColor,
      this.onPressed,
      required this.text});

  final String text;
  final Color? foregroundColor;
  final Color? backgroundColor;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
        height: 19,
        width: 19,
        child: TextButton(
            style: OutlinedButton.styleFrom(
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(0),
                ),
                foregroundColor: foregroundColor,
                padding: const EdgeInsets.symmetric(vertical: 2, horizontal: 2),
                backgroundColor: backgroundColor),
            onPressed: onPressed,
            child: Text(
              text,
              style: const TextStyle(fontSize: AppStyles.defaultFontSize),
            )));
  }
}
