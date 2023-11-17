import 'package:flutter/material.dart';

class MenuItemWidget extends StatelessWidget {
  const MenuItemWidget({super.key, required this.route, required this.navigateId, required this.text});

  final String route;
  final String navigateId;
  final String text;

  @override
  Widget build(BuildContext context) {
 return   Container(
   margin: EdgeInsets.only(left: 100, right: 100),
      width: 150,
      height: 50,
      child: TextButton(
          style: ButtonStyle(
            padding: MaterialStateProperty.all(
                const EdgeInsets.symmetric(vertical: 1, horizontal: 1)),
            foregroundColor:
            MaterialStateProperty.all<Color>(Colors.blue),
          ),
          onPressed: () {
            Navigator.pushNamed(context, navigateId);
          },
          child: Text(text,
              style: TextStyle(
                  fontSize: 15,
                  fontWeight: route == navigateId
                      ? FontWeight.bold
                      : FontWeight.normal))),
    );
  }
}