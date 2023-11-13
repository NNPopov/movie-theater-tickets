import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';

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
          color: Colors.white,
          borderRadius: BorderRadius.circular(15),
          border: Border.all(
            color: Colors.blue,
            width: 2,
          ),
        ),
        padding: EdgeInsets.all(20),
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
