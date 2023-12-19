import 'package:flutter/material.dart';

import '../../../core/res/app_styles.dart';

class MenuItemWidget extends StatelessWidget {
  const MenuItemWidget(
      {super.key,
      required this.route,
      required this.navigateId,
      required this.text});

  final String route;
  final String navigateId;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 150,
      height: 30,
      child: TextButton(
          style: ButtonStyle(
            padding: MaterialStateProperty.all(
                const EdgeInsets.symmetric(vertical: 1, horizontal: 1)),
            foregroundColor: MaterialStateProperty.all<Color>(Colors.blue),
          ),
          onPressed: () {
            Navigator.pushNamed(context, navigateId);
          },
          child: Text(text,
              style: TextStyle(
                  fontSize: AppStyles.defaultMenuFontSize,
                  fontWeight: route == navigateId
                      ? FontWeight.bold
                      : FontWeight.normal))),
    );
  }
}
